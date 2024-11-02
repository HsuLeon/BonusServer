import { login } from "@Api/test/login/login";
import { LoginRequest } from "@Type/api/Login";
import { useAuth } from "@Zustand/useAuth";
import { useRecords } from "@Zustand/useRecords";
import { useEffect, useState } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import BonusPage from "./page/BonusPage";

function App() {
  const { setToken, token, webSite, setWebSite } = useAuth();
  const { setNextRecord } = useRecords();
  const [loading, setLoading] = useState(true);

  const loginHandler = async () => {
    try {
      const req: LoginRequest = {
        machineName: "Jeff",
        scoreScale: 1,
      };

      const { token, webSite } = await login(req);
      setToken(token);
      setWebSite(webSite);
    } catch (error) {
      console.error("Login failed", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!token || !webSite) {
      loginHandler();
    } else {
      setLoading(false);
    }
  }, [token, webSite]);

  useEffect(() => {
    const handleClick = () => {
      setNextRecord();
    };

    document.addEventListener("click", handleClick);

    return () => {
      document.removeEventListener("click", handleClick);
    };
  }, []);

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <Routes>
      <Route
        path="/"
        element={
          token && webSite ? (
            <Navigate to="/bonus" replace />
          ) : (
            <div>未取得金鑰</div>
          )
        }
      />
      <Route
        path="/bonus"
        element={token && webSite ? <BonusPage /> : <Navigate to="/" replace />}
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
