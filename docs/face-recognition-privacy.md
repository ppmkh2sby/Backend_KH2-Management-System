# Face Recognition Privacy Policy

## Retention

The current implementation retains zero raw reference images for the target
`FaceProfile` workflow. `FaceProfile.ReferenceImagePath` remains nullable and
is not populated by enrollment.

The legacy account-operated enrollment flow may store up to five private,
temporary captures only while an enrollment is in progress. After an enrollment
is accepted or rejected, it deletes all capture objects and their capture
metadata. A subsequent completion request retries cleanup if a prior cleanup
was interrupted.

## Storage boundaries

- Raw images are written only through `IFaceCaptureStorage` under its configured
  private root; PostgreSQL stores no Base64 or BYTEA image values.
- Storage keys are internal implementation details and are not returned by API
  responses.
- Image extensions are derived from the validated MIME type, not client file
  names.
- The production storage root must be outside the web root and inaccessible to
  public object-storage policies.

## Exposure and logging

- Face embeddings and raw images are not returned by public API DTOs.
- Application logs must not include images, embeddings, storage keys, device
  API keys, JWTs, or upstream exception payloads.
- `FaceRecognitionEvent` stores only safe recognition outcome metadata.
