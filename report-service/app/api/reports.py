from fastapi import APIRouter, Depends, Query
from typing import Optional
from datetime import date
from services.report_generator import ReportGenerator
from core.dependencies import get_current_user_id

router = APIRouter()

@router.get("/summary")
async def get_summary_report(
    from_date: date = Query(...),
    to_date: date = Query(...),
    format: str = "json",
    user_id: str = Depends(get_current_user_id)
):
    generator = ReportGenerator()
    result = generator.get_or_generate_report(user_id, from_date.isoformat(), to_date.isoformat(), format)
    return RedirectResponse(result["url"], status_code=303)

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