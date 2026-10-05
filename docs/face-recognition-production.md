# Face Recognition Production Hardening

## Operations endpoints

- `GET /health/live` verifies that the ASP.NET process is running. It performs
  no dependency calls and is suitable for a liveness probe.
- `GET /health/ready` verifies PostgreSQL connectivity and the server-to-server
  Face Service health endpoint. It returns only standard health status; it does
  not disclose upstream errors or configuration.
- Every response contains `X-Correlation-ID`. A caller-provided value is used
  only when it is a short alphanumeric, hyphen, or underscore token; otherwise
  the API generates one. Structured logs include this value through logging
  scope and must never include images, embeddings, API keys, JWTs, passwords,
  storage keys, or raw upstream exceptions.

## Rate limits

- Internal face-recognition and enrollment routes use the existing
  `FaceRecognition` fixed-window policy: 20 requests per minute per signed-in
  user or source IP.
- `/api/attendance/face-recognition` uses the separate
  `FaceAttendanceDevice` fixed-window policy: 30 requests per minute per valid
  device ID, with IP fallback for malformed/missing IDs. It is not an
  authentication replacement; device API-key verification remains mandatory.

## Required production configuration

Use environment variables or a protected production configuration provider.
Never commit values for `ConnectionStrings__DefaultConnection`,
`Jwt__SecretKey`, `FaceRecognition__ApiKey`, or
`FaceRecognition__ServiceApiKey`.

`FaceRecognition__SimilarityThreshold` must remain unset until Phase 8
calibration supplies a measured value. Existing legacy
`FaceRecognition__ConfidenceThreshold` is independent and must not be reused
as the pgvector similarity threshold.

Set `FaceRecognition__CaptureStoragePath` outside the public web root. The
process identity must have access to that private location only.

## Load-test procedure

Install [k6](https://grafana.com/docs/k6/latest/) on an isolated test runner.
Use distinct non-production devices, a disposable Sesi, and a controlled image:

```bash
k6 run scripts/face-recognition-load-test.js \
  -e BASE_URL=https://staging-api.example.com \
  -e DEVICE_ONE_ID=... -e DEVICE_ONE_API_KEY=... \
  -e DEVICE_TWO_ID=... -e DEVICE_TWO_API_KEY=... \
  -e SESI_ID=... -e ATTENDANCE_IMAGE=./test-face.jpg
```

The scenario exercises web liveness traffic at 25, 50, 100, and 150 concurrent
virtual users plus a two-device attendance burst. Record p50/p95/p99 latency,
CPU, RAM, error rate, PostgreSQL connection usage, and Face Service latency.
Keep CPU inference on KVM 2 only when these measurements meet the agreed SLO;
otherwise move only the Face Service to GPU infrastructure. Do not run this
script against production until duplicate-event retention and attendance test
data have been approved.
