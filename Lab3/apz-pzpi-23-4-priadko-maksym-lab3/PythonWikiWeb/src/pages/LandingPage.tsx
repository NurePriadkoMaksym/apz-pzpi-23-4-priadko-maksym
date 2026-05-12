import React from "react";
import { ApiClient } from "../api";
import { AuthPanel } from "../components/AuthPanel";
import { localeOptions } from "../i18n";
import { AuthResponse, LocaleCode, Settings } from "../types";

export function LandingPage({
  api,
  msg,
  run,
  session,
  setSession,
  settings,
  setSettings
}: {
  api: ApiClient;
  msg: (key: string) => string;
  run: <T>(action: () => Promise<T>, success?: string) => Promise<T | undefined>;
  session: AuthResponse | null;
  setSession: (session: AuthResponse | null) => void;
  settings: Settings;
  setSettings: (settings: Settings) => void;
}) {
  return (
    <main className="landing-page">
      <header className="landing-nav">
        <div className="brand compact">
          <div className="brand-mark">Py</div>
          <div>
            <h1>{msg("appName")}</h1>
            <p>{msg("landingSubtitle")}</p>
          </div>
        </div>
        <select value={settings.locale} onChange={(event) => setSettings({ ...settings, locale: event.target.value as LocaleCode })}>
          {localeOptions.map((locale) => (
            <option key={locale.code} value={locale.code}>
              {locale.label}
            </option>
          ))}
        </select>
      </header>
      <section className="landing-hero">
        <div className="landing-copy">
          <p className="eyebrow">{msg("landingEyebrow")}</p>
          <h2>{msg("landingTitle")}</h2>
        </div>
        <div className="landing-card">
          <AuthPanel api={api} session={session} setSession={setSession} run={run} msg={msg} />
        </div>
      </section>
    </main>
  );
}
