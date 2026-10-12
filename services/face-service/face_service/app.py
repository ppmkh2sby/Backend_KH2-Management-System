import os
import secrets
from contextlib import asynccontextmanager

from fastapi import Depends, FastAPI, Header, HTTPException, UploadFile
from fastapi.concurrency import run_in_threadpool

from .inference import OnnxAnalyzer, rejected


def create_app(analyzer_factory=OnnxAnalyzer):
    @asynccontextmanager
    async def lifespan(app):
        key = os.environ.get("FACE_SERVICE_API_KEY", "")
        if len(key.strip()) < 32 or not key.isascii():
            raise RuntimeError("FACE_SERVICE_API_KEY must contain at least 32 ASCII characters")
        app.state.api_key = key
        try:
            app.state.analyzer = analyzer_factory()
        except Exception:
            # No Python exceptions, paths, images, embeddings or secrets reach callers/logs.
            app.state.analyzer = None
        yield
        app.state.analyzer = None

    app = FastAPI(lifespan=lifespan, docs_url=None, redoc_url=None, openapi_url=None)

    def authenticate(x_face_service_key: str | None = Header(default=None)):
        if x_face_service_key is None or not secrets.compare_digest(
            x_face_service_key.encode("utf-8"), app.state.api_key.encode("utf-8")
        ):
            raise HTTPException(status_code=401, detail="Unauthorized")

    @app.get("/v1/health", dependencies=[Depends(authenticate)])
    def health():
        ready = app.state.analyzer is not None
        return {"isHealthy": ready, "reason": None if ready else "FaceServiceUnavailable"}

    @app.post("/v1/analyze", dependencies=[Depends(authenticate)])
    async def analyze(image: UploadFile):
        try:
            if image.content_type not in ("image/jpeg", "image/png"):
                return rejected("InvalidImage")
            data = await image.read(5 * 1024 * 1024 + 1)
            if not data or len(data) > 5 * 1024 * 1024:
                return rejected("InvalidImage")
            if app.state.analyzer is None:
                return rejected("FaceServiceUnavailable")
            try:
                return await run_in_threadpool(app.state.analyzer.analyze, data)
            except Exception:
                return rejected("FaceServiceUnavailable")
        finally:
            await image.close()

    return app


app = create_app()
