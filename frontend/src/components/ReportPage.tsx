import React, { useState, useEffect } from 'react';

const AUTH_API = '/api/auth';
const BUSINESS_API = '/api';

const ReportPage: React.FC = () => {
  const [user, setUser] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [fromDate, setFromDate] = useState<string>(() => {
    const d = new Date();
    d.setDate(d.getDate() - 7);
    return d.toISOString().slice(0, 10);
  });
  const [toDate, setToDate] = useState<string>(() => new Date().toISOString().slice(0, 10));

  // Проверка сессии
  useEffect(() => {
    fetch(`${AUTH_API}/user`, { credentials: 'include' })
      .then(res => {
        if (res.status === 401) {
          setUser(null);
          return null;
        }
        return res.json();
      })
      .then(data => {
        if (data && data.username) setUser(data);
      })
      .catch(() => setUser(null));
  }, []);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      const res = await fetch(`${AUTH_API}/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ Username: username, Password: password })
      });
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || 'Login failed');
      setUser(data.user);
      setUsername('');
      setPassword('');
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleLogout = async () => {
    await fetch(`${AUTH_API}/logout`, { method: 'POST', credentials: 'include' });
    setUser(null);
  };

  const handleYandexLogin = () => {
    const keycloakAuthUrl = `http://localhost:8080/realms/reports-realm/protocol/openid-connect/auth?client_id=reports-frontend&response_type=code&redirect_uri=${window.location.origin}&kc_idp_hint=yandex`;
    window.location.href = keycloakAuthUrl;
  };

  const generateReport = async (format: 'json' | 'pdf' = 'pdf') => {
    setLoading(true);
    setError(null);
    try {
      const url = `${BUSINESS_API}/reports/summary?from_date=${fromDate}&to_date=${toDate}&format=${format}`;
      const res = await fetch(url, { credentials: 'include' });
      if (res.status === 401) {
        setUser(null);
        throw new Error('Session expired, please login again');
      }
      if (!res.ok) {
        const errText = await res.text();
        throw new Error(errText || 'Failed to generate report');
      }
      if (format === 'pdf') {
        const blob = await res.blob();
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `report_${fromDate}_${toDate}.pdf`;
        link.click();
      } else {
        const data = await res.json();
        console.log('Report data:', data);
        alert('Report data received (check console)');
      }
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  if (!user) {
    return (
      <div style={{ maxWidth: 400, margin: '50px auto' }}>
        <h2>Login</h2>
        {error && <div style={{ color: 'red' }}>{error}</div>}
        <form onSubmit={handleLogin}>
          <input
            type="text"
            placeholder="Username"
            value={username}
            onChange={e => setUsername(e.target.value)}
            style={{ display: 'block', marginBottom: 10, width: '100%' }}
          />
          <input
            type="password"
            placeholder="Password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            style={{ display: 'block', marginBottom: 10, width: '100%' }}
          />
          <button type="submit">Login</button>
        </form>
        <div style={{ marginTop: 10 }}>
          <button onClick={handleYandexLogin}>Войти через Яндекс</button>
        </div>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 600, margin: '50px auto', textAlign: 'center' }}>
      <h2>Welcome, {user.username}!</h2>
      <div style={{ margin: '20px 0' }}>
        <label>
          From:&nbsp;
          <input
            type="date"
            value={fromDate}
            onChange={e => setFromDate(e.target.value)}
          />
        </label>
        &nbsp;&nbsp;
        <label>
          To:&nbsp;
          <input
            type="date"
            value={toDate}
            onChange={e => setToDate(e.target.value)}
          />
        </label>
      </div>
      <div>
        <button onClick={() => generateReport('pdf')} disabled={loading}>
          {loading ? 'Generating PDF...' : 'Download PDF Report'}
        </button>
        &nbsp;
        <button onClick={() => generateReport('json')} disabled={loading}>
          Get JSON Report
        </button>
        &nbsp;
        <button onClick={handleLogout}>Logout</button>
      </div>
      {error && <div style={{ color: 'red', marginTop: 10 }}>{error}</div>}
    </div>
  );
};

export default ReportPage;
