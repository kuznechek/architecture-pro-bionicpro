from fastapi import APIRouter, Depends, Query
from typing import Optional
from datetime import date
from services.report_generator import ReportGenerator
from core.dependencies import get_current_user_id

router = APIRouter()

@router.get("/summary")
async def get_summary_report(
    from_date: date = Query(..., description="Start date"),
    to_date: date = Query(..., description="End date"),
    user_id: str = Depends(get_current_user_id)
):
    generator = ReportGenerator()
    data = generator.generate_summary(user_id, from_date, to_date)
    return JSONResponse(content=data)

@router.get("/detailed")
async def get_detailed_report(
    from_date: date,
    to_date: date,
    format: str = "json",
    user_id: str = Depends(get_current_user_id)
):
    generator = ReportGenerator()
    if format == "pdf":
        pdf = generator.generate_pdf(user_id, from_date, to_date)
        return StreamingResponse(pdf, media_type="application/pdf",
                                 headers={"Content-Disposition": "attachment; filename=report.pdf"})
    else:
        data = generator.generate_detailed(user_id, from_date, to_date)
        return JSONResponse(content=data)