import React from "react";
import { EmptyState } from "../components/EmptyState";
import { StatGrid } from "../components/StatGrid";
import { AdminUser, Article, Course, Role } from "../types";

export function DashboardPage({
  articles,
  courses,
  msg,
  role,
  users
}: {
  articles: Article[];
  courses: Course[];
  msg: (key: string) => string;
  role: Role;
  users: AdminUser[];
}) {
  const locked = articles.filter((article) => article.isLocked).length;
  const titleByRole: Record<Role, string> = {
    Reader: msg("readerDashboard"),
    Editor: msg("editorDashboard"),
    Creator: msg("creatorDashboard"),
    Admin: msg("adminDashboard")
  };

  return (
    <div className="page-stack">
      <StatGrid
        items={[
          { label: msg("articles"), value: articles.length, tone: "blue" },
          { label: msg("courses"), value: courses.length, tone: "green" },
          { label: msg("locked"), value: locked, tone: "amber" },
          ...(role === "Admin" ? [{ label: msg("users"), value: users.length, tone: "red" as const }] : [])
        ]}
      />
      <section className="panel dashboard-panel">
        <div>
          <p className="eyebrow">{role}</p>
          <h3>{titleByRole[role]}</h3>
          <p>{msg(`dashboardText${role}`)}</p>
        </div>
        {articles.length === 0 && courses.length === 0 && <EmptyState title={msg("noSession")} text={msg("refresh")} />}
      </section>
    </div>
  );
}
