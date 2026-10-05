from types import SimpleNamespace
from threading import Lock

import cv2
import numpy as np
import pytest
from fastapi.testclient import TestClient

from face_service.app import create_app
from face_service.inference import OnnxAnalyzer, align, rejected

KEY = "t" * 32


@pytest.fixture(autouse=True)
def key(monkeypatch):
    monkeypatch.setenv("FACE_SERVICE_API_KEY", KEY)


class FakeAnalyzer:
    def analyze(self, data):
        return {"isAccepted": True, "faceCount": 1, "embedding": [1.0] + [0.0] * 511,
                "qualityScore": None, "reason": None, "modelName": "arcface", "modelVersion": "test"}


def test_authentication_and_success():
    with TestClient(create_app(FakeAnalyzer)) as client:
        assert client.get("/v1/health").status_code == 401
        headers = {"X-Face-Service-Key": KEY}
        assert client.get("/v1/health", headers=headers).json()["isHealthy"]
        response = client.post("/v1/analyze", files={"image": ("a.jpg", b"image", "image/jpeg")}, headers=headers)
        assert response.json()["isAccepted"]
        assert len(response.json()["embedding"]) == 512


def test_models_loaded_only_once_and_invalid_upload():
    calls = []
    def factory():
        calls.append(1)
        return FakeAnalyzer()
    with TestClient(create_app(factory)) as client:
        headers = {"X-Face-Service-Key": KEY}
        for _ in range(2):
            assert client.post("/v1/analyze", files={"image": ("a.txt", b"x", "text/plain")}, headers=headers).json()["reason"] == "InvalidImage"
        assert calls == [1]


def test_model_failure_is_safe():
    def broken():
        raise ValueError("private path and exception")
    with TestClient(create_app(broken)) as client:
        headers = {"X-Face-Service-Key": KEY}
        assert client.get("/v1/health", headers=headers).json() == {"isHealthy": False, "reason": "FaceServiceUnavailable"}
        result = client.post("/v1/analyze", files={"image": ("a.jpg", b"x", "image/jpeg")}, headers=headers).json()
        assert result == rejected("FaceServiceUnavailable")


def test_secret_required(monkeypatch):
    monkeypatch.delenv("FACE_SERVICE_API_KEY")
    with pytest.raises(RuntimeError):
        with TestClient(create_app(FakeAnalyzer)):
            pass


def png():
    return cv2.imencode(".png", np.zeros((112, 112, 3), dtype=np.uint8))[1].tobytes()


def pipeline(count=1):
    engine = object.__new__(OnnxAnalyzer)
    engine.lock = Lock()
    engine.min_face_pixels = 0
    engine.min_sharpness = 0
    engine.model_name = "arcface"
    engine.model_version = "test"
    points = [[38.2946, 51.6963], [73.5318, 51.5014], [56.0252, 71.7366], [41.5493, 92.3655], [70.7299, 92.2041]]
    engine._detect = lambda image: ([[0, 0, 100, 100]] * count, [points] * count)
    engine.arcface_input = SimpleNamespace(name="input")
    engine.arcface = SimpleNamespace(run=lambda outputs, inputs: [np.ones((1, 512), dtype=np.float32)])
    return engine


@pytest.mark.parametrize("count,reason", [(0, "NoFaceDetected"), (2, "MultipleFacesDetected")])
def test_face_count_gates(count, reason):
    assert pipeline(count).analyze(png())["reason"] == reason


def test_pipeline_normalizes_and_validates_embedding():
    engine = pipeline()
    result = engine.analyze(png())
    assert result["isAccepted"]
    assert np.isclose(np.linalg.norm(result["embedding"]), 1.0)
    engine.arcface = SimpleNamespace(run=lambda outputs, inputs: [np.zeros((1, 512), dtype=np.float32)])
    with pytest.raises(ValueError):
        engine.analyze(png())


def test_invalid_image_quality_and_landmarks():
    engine = pipeline()
    assert engine.analyze(b"invalid")["reason"] == "InvalidImage"
    engine.min_sharpness = 1
    assert engine.analyze(png())["reason"] == "PoorImageQuality"
    with pytest.raises(ValueError):
        align(np.zeros((112, 112, 3), dtype=np.uint8), np.zeros((5, 2)))


def test_scrfd_decodes_tensors():
    engine = pipeline()
    engine.detection_threshold = 0.5
    outputs = []
    for width in (1, 4, 10):
        for stride in (8, 16, 32):
            outputs.append(np.zeros(((640 // stride) ** 2 * 2, width), dtype=np.float32))
    outputs[0][1000] = 0.9
    outputs[3][1000] = 4
    engine.detector_input = SimpleNamespace(name="input")
    engine.detector = SimpleNamespace(run=lambda names, inputs: outputs)
    boxes, landmarks = OnnxAnalyzer._detect(engine, np.zeros((640, 640, 3), dtype=np.uint8))
    assert len(boxes) == 1
    assert np.asarray(landmarks).shape == (1, 5, 2)
