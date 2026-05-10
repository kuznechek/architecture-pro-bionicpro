import pandas as pd
from io import BytesIO
from reportlab.lib.pagesizes import A4
from reportlab.pdfgen import canvas
from db.clickhouse import ch_client

class ReportGenerator:
    def generate_summary(self, user_id: str, from_date, to_date):
        query = """
            SELECT user_name, prosthesis_id,
                sum(total_signals) as total_signals,
                avg(avg_signal_strength) as avg_strength,
                sum(active_minutes) as total_active_minutes,
                sum(error_count) as total_errors
            FROM bionicpro.report_fact
            WHERE user_id = %(user_id)s AND date BETWEEN %(from)s AND %(to)s
            GROUP BY user_name, prosthesis_id
        """
        result = ch_client.execute(query, {'user_id': user_id, 'from': from_date, 'to': to_date})
        columns = ['user_name', 'prosthesis_id', 'total_signals', 'avg_strength', 'total_active_minutes', 'total_errors']
        return [dict(zip(columns, row)) for row in result]
    
    def generate_pdf(self, user_id, from_date, to_date):
        data = self.generate_summary(user_id, from_date, to_date)
        buffer = BytesIO()
        p = canvas.Canvas(buffer, pagesize=A4)
        p.drawString(100, 800, f"Report for user {user_id} from {from_date} to {to_date}")
        y = 750
        for row in data:
            p.drawString(100, y, f"Prosthesis {row['prosthesis_id']}: active {row['total_active_minutes']} min, errors {row['total_errors']}")
            y -= 20
        p.save()
        buffer.seek(0)
        return buffer