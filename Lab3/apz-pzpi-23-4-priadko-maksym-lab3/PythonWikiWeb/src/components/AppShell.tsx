import React from "react";
import { AuthPanel } from "./AuthPanel";
import { ApiClient } from "../api";
import { formatDate, localeOptions } from "../i18n";
import { AuthResponse, LocaleCode, Settings } from "../types";

export interface NavItem {
  id: string;
  label: string;
}

interface AppShellProps {
  active: string;
  api: ApiClient;
  children: React.ReactNode;
  loadedAt: string | null;
  msg: (key: string) => string;
  navItems: NavItem[];
  onNavigate: (id: string) => void;
  onRefresh: () => void;
  run: <T>(action: () => Promise<T>, success?: string) => Promise<T | undefined>;
  session: AuthResponse | null;
  setSession: (session: AuthResponse | null) => void;
  settings: Settings;
  setSettings: (settings: Settings) => void;
  status: string;
}

export function AppShell({
  active,
  api,
  children,
  loadedAt,
  msg,
  navItems,
  onNavigate,
  onRefresh,
  run,
  session,
  setSession,
  settings,
  setSettings,
  status
}: AppShellProps) {
  const activeLabel = navItems.find((item) => item.id === active)?.label ?? msg("appName");

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-mark">Py</div>
          <div>
            <h1>{msg("appName")}</h1>
            <p>{session ? `${session.username} / ${session.role}` : msg("noSession")}</p>
          </div>
        </div>
        <nav className="main-nav">
          {navItems.map((item) => (
            <button key={item.id} className={active === item.id ? "active" : ""} onClick={() => onNavigate(item.id)}>
              <span>{item.label}</span>
            </button>
          ))}
        </nav>
        <section className="locale-panel">
          <label>
            {msg("language")}
            <select
              value={settings.locale}
              onChange={(event) => setSettings({ ...settings, locale: event.target.value as LocaleCode })}
            >
              {localeOptions.map((locale) => (
                <option key={locale.code} value={locale.code}>
                  {locale.label}
                </option>
              ))}
            </select>
          </label>
        </section>
      </aside>
      <section className="workspace">
        <header className="topbar">
          <div>
            <p className="eyebrow">{msg("appName")}</p>
            <h2>{activeLabel}</h2>
            <span>{loadedAt ? `${msg("loadedAt")}: ${formatDate(settings.locale, loadedAt)}` : msg("statusReady")}</span>
          </div>
          <div className="topbar-actions">
            <button className="secondary-button" onClick={onRefresh} disabled={!session}>
              {msg("refresh")}
            </button>
            <AuthPanel api={api} session={session} setSession={setSession} run={run} msg={msg} />
          </div>
        </header>
        {status && <div className="status-banner">{status}</div>}
        {children}
      </section>
    </main>
  );
}
