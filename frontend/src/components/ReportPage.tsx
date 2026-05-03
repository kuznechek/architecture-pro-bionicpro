import React, { useState, useEffect } from 'react';

const AUTH_API = process.env.REACT_APP_AUTH_API_URL || 'https://localhost:5001/api/auth';
const BUSINESS_API = process.env.REACT_APP_BUSINESS_API_URL || 'https://localhost:5001/api';

const ReportPage: React.FC = () => {
  const [user, setUser] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');

  useEffect(() => {
    fetch(`${AUTH_API}/user`, { credentials: 'include' })
      .then(res => res.ok ? res.json() : null)
      .then(data => setUser(data))
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
        body: JSON.stringify({ username, password })
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

  const downloadReport = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(`${BUSINESS_API}/reports`, { credentials: 'include' });
      if (res.status === 401) {
        setUser(null);
        throw new Error('Session expired, please login again');
      }
      if (!res.ok) throw new Error('Failed to get report');
      const blob = await res.blob();
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = 'report.pdf';
      link.click();
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
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 400, margin: '50px auto', textAlign: 'center' }}>
      <h2>Welcome, {user.username}!</h2>
      <button onClick={downloadReport} disabled={loading}>
        {loading ? 'Generating...' : 'Download Report'}
      </button>
      <button onClick={handleLogout} style={{ marginLeft: 10 }}>Logout</button>
      {error && <div style={{ color: 'red', marginTop: 10 }}>{error}</div>}
    </div>
  );
};

export default ReportPage;