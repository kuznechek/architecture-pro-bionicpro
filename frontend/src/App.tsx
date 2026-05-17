import React from 'react';
import ReportPage from './components/ReportPage';

// const keycloakConfig: KeycloakConfig = {
//   url: process.env.REACT_APP_KEYCLOAK_URL,
//   realm: process.env.REACT_APP_KEYCLOAK_REALM||"",
//   clientId: process.env.REACT_APP_KEYCLOAK_CLIENT_ID||"",
//   pkceMethod: 'S256'
// };

const App: React.FC = () => {
  return (
    <div className="App">
      <ReportPage />
    </div>
  );
};

export default App;