"""CPU-only SCRFD (9-output, 2 anchors) and ArcFace ONNX pipeline."""
import hashlib
import io
import os
from pathlib import Path
from threading import Lock

import cv2
import numpy as np
from PIL import Image


class OnnxAnalyzer:
    def __init__(self):
        import onnxruntime as ort

        detector_path = Path(os.environ["FACE_DETECTOR_PATH"])
        arcface_path = Path(os.environ["FACE_ARCFACE_PATH"])
        self.model_name = os.environ.get("FACE_MODEL_NAME", "arcface")
        # Bind the version to the actual weights; do not silently mix model artifacts.
        artifact_hashes = hashlib.sha256(detector_path.read_bytes()).digest() + hashlib.sha256(arcface_path.read_bytes()).digest()
        self.model_version = hashlib.sha256(artifact_hashes + b"scrfd640-arcface112-rgb-v1").hexdigest()
        if not self.model_name.strip() or len(self.model_name) > 100:
            raise ValueError("Invalid model name")
        threads = int(os.environ.get("FACE_CPU_THREADS", "2"))
        if threads < 1:
            raise ValueError("Invalid thread count")
        opts = ort.SessionOptions()
        opts.intra_op_num_threads = threads
        self.detector = ort.InferenceSession(str(detector_path), opts, providers=["CPUExecutionProvider"])
        self.arcface = ort.InferenceSession(str(arcface_path), opts, providers=["CPUExecutionProvider"])
        self.detector_input = self.detector.get_inputs()[0]
        self.arcface_input = self.arcface.get_inputs()[0]
        if self.detector_input.type != "tensor(float)" or self.arcface_input.type != "tensor(float)":
            raise ValueError("Models must accept float32 tensors")
        if len(self.detector.get_outputs()) != 9 or len(self.arcface.get_outputs()) != 1:
            raise ValueError("Unsupported ONNX export")
        if self.arcface_input.shape != [1, 3, 112, 112]:
            raise ValueError("ArcFace must accept float RGB NCHW 1x3x112x112")
        if self.arcface.get_outputs()[0].shape != [1, 512]:
            raise ValueError("ArcFace must produce 512 dimensions")
        detector_shape = self.detector_input.shape
        if len(detector_shape) != 4 or detector_shape[1] != 3:
            raise ValueError("SCRFD must accept NCHW images")
        if any(isinstance(axis, int) and axis != 640 for axis in detector_shape[2:]):
            raise ValueError("SCRFD spatial input must be dynamic or 640x640")
        self.detection_threshold = float(os.environ.get("FACE_DETECTION_THRESHOLD", "0.5"))
        self.min_sharpness = float(os.environ.get("FACE_MIN_SHARPNESS", "0"))
        self.min_face_pixels = int(os.environ.get("FACE_MIN_FACE_PIXELS", "0"))
        if not np.isfinite(self.detection_threshold) or not 0 < self.detection_threshold < 1:
            raise ValueError("Invalid detection threshold")
        if not np.isfinite(self.min_sharpness) or self.min_sharpness < 0 or self.min_face_pixels < 0:
            raise ValueError("Invalid quality gate")
        self.lock = Lock()

    def analyze(self, data: bytes) -> dict:
        try:
            with Image.open(io.BytesIO(data)) as header:
                if header.format not in ("JPEG", "PNG") or header.width * header.height > 12_000_000:
                    return rejected("InvalidImage")
            image = cv2.imdecode(np.frombuffer(data, dtype=np.uint8), cv2.IMREAD_COLOR)
            if image is None or image.size == 0:
                return rejected("InvalidImage")
        except (OSError, ValueError, Image.DecompressionBombError):
            return rejected("InvalidImage")
        with self.lock:
            boxes, landmarks = self._detect(image)
            if len(boxes) == 0:
                return rejected("NoFaceDetected", 0)
            if len(boxes) != 1:
                return rejected("MultipleFacesDetected", len(boxes))
            box = boxes[0]
            if min(box[2] - box[0], box[3] - box[1]) < self.min_face_pixels:
                return rejected("PoorImageQuality", 1)
            crop = align(image, landmarks[0])
            if cv2.Laplacian(cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY), cv2.CV_64F).var() < self.min_sharpness:
                return rejected("PoorImageQuality", 1)
            blob = cv2.dnn.blobFromImage(crop, 1.0 / 127.5, (112, 112), (127.5, 127.5, 127.5), swapRB=True)
            vector = np.asarray(self.arcface.run(None, {self.arcface_input.name: blob})[0], dtype=np.float32).reshape(-1)
            norm = float(np.linalg.norm(vector))
            if vector.size != 512 or not np.isfinite(vector).all() or not np.isfinite(norm) or norm <= 0:
                raise ValueError("Invalid embedding output")
            vector /= norm
            return {"isAccepted": True, "faceCount": 1, "embedding": vector.tolist(),
                    "qualityScore": None, "reason": None, "modelName": self.model_name,
                    "modelVersion": self.model_version}

    def _detect(self, image):
        height, width = image.shape[:2]
        scale = min(640 / width, 640 / height)
        resized = cv2.resize(image, (max(1, int(width * scale)), max(1, int(height * scale))))
        canvas = np.zeros((640, 640, 3), dtype=np.uint8)
        canvas[:resized.shape[0], :resized.shape[1]] = resized
        blob = cv2.dnn.blobFromImage(canvas, 1.0 / 128, (640, 640), (127.5, 127.5, 127.5), swapRB=True)
        outputs = self.detector.run(None, {self.detector_input.name: blob})
        all_boxes, all_scores, all_landmarks = [], [], []
        for level, stride in enumerate((8, 16, 32)):
            grid = 640 // stride
            y, x = np.mgrid[:grid, :grid]
            centers = np.stack((x, y), axis=-1).reshape(-1, 2).repeat(2, axis=0) * stride
            scores = np.asarray(outputs[level]).reshape(-1)
            distances = np.asarray(outputs[level + 3]).reshape(-1, 4) * stride
            points = np.asarray(outputs[level + 6]).reshape(-1, 5, 2) * stride
            if len(scores) != len(centers) or len(distances) != len(centers) or len(points) != len(centers):
                raise ValueError("Unsupported SCRFD tensor layout")
            if not all(np.isfinite(array).all() for array in (scores, distances, points)):
                raise ValueError("Invalid detector output")
            selected = scores >= self.detection_threshold
            centers, distances, points = centers[selected], distances[selected], points[selected]
            boxes = np.column_stack((centers[:, 0] - distances[:, 0], centers[:, 1] - distances[:, 1],
                                     centers[:, 0] + distances[:, 2], centers[:, 1] + distances[:, 3])) / scale
            all_boxes.extend(boxes.tolist())
            all_scores.extend(scores[selected].tolist())
            all_landmarks.extend(((points + centers[:, None, :]) / scale).tolist())
        if not all_boxes:
            return [], []
        xywh = [[b[0], b[1], b[2] - b[0], b[3] - b[1]] for b in all_boxes]
        kept = np.asarray(cv2.dnn.NMSBoxes(xywh, all_scores, self.detection_threshold, 0.4)).reshape(-1)
        return [all_boxes[i] for i in kept], [all_landmarks[i] for i in kept]


def align(image, landmarks):
    # Standard ArcFace 112px template. Similarity transform preserves face geometry.
    target = np.array([[38.2946, 51.6963], [73.5318, 51.5014], [56.0252, 71.7366],
                       [41.5493, 92.3655], [70.7299, 92.2041]], dtype=np.float64)
    source = np.asarray(landmarks, dtype=np.float64)
    if source.shape != (5, 2) or not np.isfinite(source).all():
        raise ValueError("Invalid face landmarks")
    source_mean, target_mean = source.mean(axis=0), target.mean(axis=0)
    centered_source, centered_target = source - source_mean, target - target_mean
    variance = np.sum(centered_source ** 2) / 5
    if variance <= 0:
        raise ValueError("Degenerate face landmarks")
    u, singular, vt = np.linalg.svd(centered_target.T @ centered_source / 5)
    signs = np.array([1.0, np.sign(np.linalg.det(u @ vt))])
    rotation = u @ np.diag(signs) @ vt
    scale = (singular @ signs) / variance
    matrix = np.column_stack((scale * rotation, target_mean - scale * rotation @ source_mean))
    return cv2.warpAffine(image, matrix, (112, 112), borderValue=0)


def rejected(reason: str, count=None):
    return {"isAccepted": False, "faceCount": count, "reason": reason, "embedding": None,
            "qualityScore": None, "modelName": None, "modelVersion": None}
