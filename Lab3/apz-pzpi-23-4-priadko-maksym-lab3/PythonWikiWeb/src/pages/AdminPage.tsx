import React from "react";
import { ApiClient } from "../api";
import { StatGrid } from "../components/StatGrid";
import { AdminUser, Role } from "../types";

export function AdminPage({
  api,
  msg,
  reload,
  roles,
  run,
  users
}: {
  api: ApiClient;
  msg: (key: string) => string;
  reload: () => Promise<void>;
  roles: Role[];
  run: <T>(action: () => Promise<T>, success?: string) => Promise<T | undefined>;
  users: AdminUser[];
}) {
  return (
    <div className="page-stack">
      <StatGrid
        items={[
          { label: msg("users"), value: users.length, tone: "blue" },
          { label: msg("roles"), value: roles.length, tone: "green" },
          { label: msg("xp"), value: users.reduce((sum, user) => sum + user.xp, 0), tone: "amber" }
        ]}
      />
      <div className="content-grid admin-grid single">
        <section className="panel">
          <div className="panel-header">
            <div>
              <p className="eyebrow">{msg("adminWorkspace")}</p>
              <h3>{msg("users")}</h3>
            </div>
          </div>
          <div className="user-table">
            {users.map((user) => (
              <div className="user-row" key={user.id}>
                <div className="avatar small">{user.username.slice(0, 2).toUpperCase()}</div>
                <div>
                  <strong>{user.username}</strong>
                  <span>{user.email}</span>
                </div>
                <span>{user.xp} XP</span>
                <select
                  value={user.role}
                  onChange={async (event) => {
                    await run(() => api.changeRole(user.id, event.target.value as Role));
                    await reload();
                  }}
                >
                  {roles.map((role) => (
                    <option key={role} value={role}>
                      {role}
                    </option>
                  ))}
                </select>
                <button
                  className="danger-button"
                  onClick={async () => {
                    await run(() => api.deleteUser(user.id));
                    await reload();
                  }}
                >
                  {msg("delete")}
                </button>
              </div>
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}
