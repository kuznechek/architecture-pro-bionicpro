import boto3
from botocore.client import Config
from core.config import settings

class MinioClient:
    def __init__(self):
        self.client = boto3.client(
            's3',
            endpoint_url=f"http://{settings.MINIO_ENDPOINT}",
            aws_access_key_id=settings.MINIO_ACCESS_KEY,
            aws_secret_access_key=settings.MINIO_SECRET_KEY,
            config=Config(signature_version='s3v4'),
            region_name='us-east-1'
        )
        self.bucket = settings.MINIO_BUCKET

    def report_exists(self, key: str) -> bool:
        try:
            self.client.head_object(Bucket=self.bucket, Key=key)
            return True
        except:
            return False

    def upload_report(self, key: str, data: bytes, content_type: str):
        self.client.put_object(Bucket=self.bucket, Key=key, Body=data, ContentType=content_type)

    def get_cdn_url(self, key: str) -> str:
        return f"{settings.CDN_BASE_URL}/{key}"