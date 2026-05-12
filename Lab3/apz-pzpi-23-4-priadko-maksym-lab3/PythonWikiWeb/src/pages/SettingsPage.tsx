import React from "react";
import { formatDate } from "../i18n";
import { Settings } from "../types";

export function SettingsPage({
  msg,
  settings,
  setSettings
}: {
  msg: (key: string) => string;
  settings: Settings;
  setSettings: (settings: Settings) => void;
}) {
  return (
    <section className="panel settings-panel">
      <div className="panel-header">
        <div>
          <p className="eyebrow">{msg("settings")}</p>
          <h3>{msg("apiBaseUrl")}</h3>
        </div>
      </div>
      <label className="field">
        <span>{msg("apiBaseUrl")}</span>
        <input
          value={settings.apiBaseUrl}
          onChange={(event) => setSettings({ ...settings, apiBaseUrl: event.target.value })}
            placeholder="https://localhost:7211"
        />
      </label>
      <div className="preview-strip">
        <span>{msg("datePreview")}</span>
        <strong>{formatDate(settings.locale, new Date())}</strong>
      </div>
    </section>
  );
}
