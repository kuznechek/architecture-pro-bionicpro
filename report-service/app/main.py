from fastapi import FastAPI, Depends
from core.dependencies import verify_user_id
from api import reports

app = FastAPI(title="BionicPRO Report Service")

app.include_router(
    reports.router,
    prefix="/reports",
    tags=["reports"],
    dependencies=[Depends(verify_user_id)]
)

@app.get("/health")
async def health():
    return {"status": "ok"}
