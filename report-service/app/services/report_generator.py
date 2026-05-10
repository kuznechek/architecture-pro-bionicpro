from services.minio_client import MinioClient
import json
from fastapi.responses import RedirectResponse

class ReportGenerator:
    def __init__(self):
        self.minio = MinioClient()

    def get_or_generate_report(self, user_id: str, from_date: str, to_date: str, format: str):
        key = f"{user_id}/{from_date}_{to_date}.{format}"
        if self.minio.report_exists(key):
            return {"url": self.minio.get_cdn_url(key)}
        else:
            if format == 'pdf':
                data = self.generate_pdf(user_id, from_date, to_date)
                self.minio.upload_report(key, data, 'application/pdf')
            else:
                data = json.dumps(self.generate_json(user_id, from_date, to_date)).encode('utf-8')
                self.minio.upload_report(key, data, 'application/json')
            return {"url": self.minio.get_cdn_url(key)}
