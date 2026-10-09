# Legacy Face API cutover

`LegacyFaceApi__Enabled` controls public access to the staged enrollment and
FaceAttendanceSession APIs. It defaults to `true` for compatibility. It is
independent of `LegacyFaceProvider`, which configures only the transitional
provider and capture storage.

1. Deploy C2 with `LegacyFaceApi__Enabled=true` and observe structured
   `LegacyFaceApiUsage` telemetry (route, method, status only).
2. Migrate known clients: canonical enrollment captures five images in memory,
   allows retry/replacement, then sends all five in one multipart request to
   `POST /api/v1/face-profiles/me/enrollment`. Discard images after success or
   cancellation; do not persist them in local storage without a security review.
3. Observe remaining external usage; its duration is an operational decision.
4. Communicate any sunset separately; this document sets no date.
5. Explicitly deploy `LegacyFaceApi__Enabled=false`.
6. Observe stale callers receiving `410 Gone` and `Deprecation: true`.
7. Verify canonical runtime health without `LegacyFaceProvider` configuration.
8. Obtain explicit approval before C3 removes controllers/runtime, C4 removes
   legacy EF entities/configuration, Phase D removes only the five experimental
   Face migrations, and Phase E generates/reviews `AddCanonicalFaceRecognition`
   before the first explicit Supabase V2 schema write.

`FaceAttendanceSession.Id` must not be substituted with `Sesi.Id`: their
semantics differ. Opener verification, awaiting/open/closed lifecycle, Kelas,
timestamps, and legacy records responses are deprecated without a direct
canonical replacement.
