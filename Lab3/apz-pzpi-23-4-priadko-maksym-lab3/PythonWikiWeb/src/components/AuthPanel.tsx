import React, { FormEvent, useState } from "react";
import { ApiClient } from "../api";
import { AuthResponse } from "../types";

interface AuthPanelProps {
  api: ApiClient;
  session: AuthResponse | null;
  setSession: (session: AuthResponse | null) => void;
  run: <T>(action: () => Promise<T>, success?: string) => Promise<T | undefined>;
  msg: (key: string) => string;
}

export function AuthPanel({ api, session, setSession, run, msg }: AuthPanelProps) {
  const [mode, setMode] = useState<"login" | "register">("login");
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authError, setAuthError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    setAuthError("");
    try {
      const auth = mode === "login" ? await api.login(email, password) : await api.register(username, email, password);
      setSession(auth);
      setEmail("");
      setPassword("");
      setUsername("");
    } catch (error) {
      setAuthError(error instanceof Error ? error.message : String(error));
    }
  }

  if (session) {
    return (
      <div className="session-card">
        <div className="avatar">{session.username.slice(0, 2).toUpperCase()}</div>
        <div>
          <strong>{session.username}</strong>
          <span className="role-badge">{session.role}</span>
        </div>
        <button className="ghost-button" onClick={async () => {
          await run(() => api.logout(session.refreshToken));
          setSession(null);
        }}>
          {msg("logout")}
        </button>
      </div>
    );
  }

  return (
    <form className="auth-card" onSubmit={submit}>
      <div className="auth-title">
        <strong>{mode === "login" ? msg("signIn") : msg("register")}</strong>
      </div>
      <div className="segmented">
        <button type="button" className={mode === "login" ? "selected" : ""} onClick={() => setMode("login")}>
          {msg("signIn")}
        </button>
        <button type="button" className={mode === "register" ? "selected" : ""} onClick={() => setMode("register")}>
          {msg("register")}
        </button>
      </div>
      {mode === "register" && (
        <input value={username} onChange={(event) => setUsername(event.target.value)} placeholder={msg("username")} required />
      )}
      <input value={email} onChange={(event) => setEmail(event.target.value)} placeholder={msg("email")} type="email" required />
      <input value={password} onChange={(event) => setPassword(event.target.value)} placeholder={msg("password")} type="password" required />
      {authError && <p className="auth-error">{authError}</p>}
      <button>{mode === "login" ? msg("signIn") : msg("register")}</button>
    </form>
  );
}
