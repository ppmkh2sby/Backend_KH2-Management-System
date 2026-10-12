# KH2 private face analysis service

Phase 3 uses FastAPI and ONNX Runtime CPU. It runs detection, five-landmark
alignment and ArcFace inference; it never trains, stores images, identifies a
Santri, connects to Supabase, or writes attendance. Models load once per process.

## Local setup

Use Python 3.11–3.13. From this directory:

```powershell
python -m venv .venv
.venv/Scripts/python.exe -m pip install -r requirements-dev.txt
.venv/Scripts/python.exe -m pytest -q
```

Place approved model artifacts in the ignored `models/` directory. No weights
are bundled or automatically downloaded. Review the license of the actual model
artifacts before deployment. The service accepts the specific export contract
below, not an arbitrary ONNX file merely renamed to match the paths.

Set `FACE_SERVICE_API_KEY` through your secret manager/environment (32+ ASCII
characters). Set `FACE_DETECTOR_PATH` and `FACE_ARCFACE_PATH` to local absolute
paths. Optional configuration:

| Variable | Default | Meaning |
| --- | --- | --- |
| FACE_MODEL_NAME | arcface | Embedding model label, at most 100 characters |
| FACE_CPU_THREADS | 2 | ONNX intra-op thread count |
| FACE_DETECTION_THRESHOLD | 0.5 | Detector confidence, unrelated to identity similarity |
| FACE_MIN_FACE_PIXELS | 0 | Minimum detected face side; 0 disables this optional gate |
| FACE_MIN_SHARPNESS | 0 | Aligned-crop Laplacian variance minimum; 0 disables this optional gate |

Quality gates need KH2 validation data before production use. Do not treat the
defaults as validated enrollment quality or identity thresholds.

```powershell
.venv/Scripts/python.exe -m uvicorn face_service.app:app --host 127.0.0.1 --port 8010 --workers 1 --no-access-log
```

Use a private network if the backend is in another container. Do not expose the
service through the public frontend. Set the .NET `FaceRecognition:BaseUrl`
(environment `FaceRecognition__BaseUrl`) to this service and configure its key
through `FaceRecognition__ApiKey`. The older provider-ID service is configured
separately through `LegacyFaceProvider__BaseUrl` and
`LegacyFaceProvider__ApiKey`; it does not share a configuration section with
the canonical embedding service.

No model paths or compatible models means health reports unavailable. Invalid API
key configuration prevents startup. Service errors contain only known failure codes.
No images/embeddings/API keys are logged. Temporary multipart uploads are closed
and removed after each request; no reference image is retained.

## Model export contract

SCRFD detector: one float32 NCHW RGB input with shape [1,3,640,640] or dynamic
spatial dimensions, nine outputs ordered as scores at strides 8/16/32, box
distances at those strides, then ten landmark offsets at those strides. There are
two anchors per grid location. Tensors may include a singleton batch dimension.
Scores are probabilities; distances and offsets are stride-scaled. Resize with
aspect preserved, top-left-pad to 640, and normalize `(RGB-127.5)/128`. Decode
all faces and apply NMS with IoU 0.4; reject zero or multiple surviving faces.

ArcFace: one float32 [1,3,112,112] RGB input, normalized `(RGB-127.5)/127.5`;
one [1,512] output. Exports that already normalize pixels internally are not
supported. Align the five SCRFD landmarks with a similarity transform to the
112px ArcFace template, validate finite/nonzero output, and L2-normalize it.
ModelVersion is a SHA-256 fingerprint of both model files and the preprocessing
contract, preventing silent mixing of different model pipelines.

These layouts follow the [InsightFace SCRFD implementation](https://github.com/deepinsight/insightface/blob/master/python-package/insightface/model_zoo/scrfd.py)
and [ArcFace adapter](https://github.com/deepinsight/insightface/blob/master/python-package/insightface/model_zoo/arcface_onnx.py).
SCRFD is the chosen face detector for this implementation. A future YOLO detector
needs an adapter with compatible landmarks; YOLO never classifies identities.

## Internal protocol

Both endpoints require `X-Face-Service-Key`.

- GET /v1/health returns `{ "isHealthy": true, "reason": null }`, or false with FaceServiceUnavailable.
- POST /v1/analyze accepts one multipart `image` (JPEG/PNG, maximum 5 MiB, maximum 12 million decoded pixels).
- Accepted JSON contains isAccepted=true, faceCount=1, embedding (512 finite floats), qualityScore=null, reason=null, modelName and modelVersion.
- Rejected JSON contains isAccepted=false, reason, optional faceCount, and null embedding/model fields.
- Reasons are NoFaceDetected, MultipleFacesDetected, PoorImageQuality, InvalidImage and FaceServiceUnavailable.

Only the private backend receives embeddings. HTTP errors, timeouts, malformed or
oversized responses are mapped by the .NET adapter to typed failures. A reverse
proxy must enforce total request size and private reachability in production.

## Tests and limits

Tests use synthetic images and fake model sessions, exercising auth, lifecycle,
decode, count rejection, alignment, optional quality gates and normalization.
They establish neither recognition accuracy nor latency with real weights.
There is no liveness check or cross-sample same-person verification in Phase 3.
Live PostgreSQL vector serialization, real-weight CPU benchmarking, KH2 quality
calibration and production limits remain deployment validation work.
