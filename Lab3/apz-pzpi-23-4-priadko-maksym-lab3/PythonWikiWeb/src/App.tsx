import React, { useEffect, useMemo, useState } from "react";
import { ApiClient } from "./api";
import { AppShell } from "./components/AppShell";
import { EmptyState } from "./components/EmptyState";
import { sortText, t } from "./i18n";
import { getNavItems } from "./navigation";
import { AdminPage } from "./pages/AdminPage";
import { ArticleEditorPage } from "./pages/ArticleEditorPage";
import { ArticlesPage } from "./pages/ArticlesPage";
import { CourseEditorPage } from "./pages/CourseEditorPage";
import { CoursesPage } from "./pages/CoursesPage";
import { DashboardPage } from "./pages/DashboardPage";
import { LandingPage } from "./pages/LandingPage";
import { SettingsPage } from "./pages/SettingsPage";
import { readJson } from "./utils/appUtils";
import { parseGraphMlEdges } from "./utils/graphParsing";
import { AdminUser, Article, ArticleEdge, AuthResponse, Course, Role, Settings } from "./types";

const defaultSettings: Settings = {
  locale: "uk-UA",
  apiBaseUrl: ""
};

const roleOrder: Role[] = ["Reader", "Editor", "Creator", "Admin"];

export default function App() {
  const [settings, setSettings] = useState<Settings>(() => readJson("pythonWiki.settings", defaultSettings));
  const [session, setSession] = useState<AuthResponse | null>(() => readJson<AuthResponse | null>("pythonWiki.session", null));
  const [status, setStatus] = useState("");
  const [active, setActive] = useState("articles");
  const [articles, setArticles] = useState<Article[]>([]);
  const [articleEdges, setArticleEdges] = useState<ArticleEdge[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [roles, setRoles] = useState<Role[]>(roleOrder);
  const [selectedArticle, setSelectedArticle] = useState<Article | null>(null);
  const [loadedAt, setLoadedAt] = useState<string | null>(null);

  const api = useMemo(
    () => new ApiClient(() => settings.apiBaseUrl || window.location.origin, () => session?.token ?? null),
    [settings.apiBaseUrl, session?.token]
  );
  const msg = (key: string) => t(settings.locale, key);
  const canEdit = session && ["Editor", "Creator", "Admin"].includes(session.role);
  const canCreateCourse = session && ["Creator", "Admin"].includes(session.role);
  const isAdmin = session?.role === "Admin";

  const navItems = getNavItems(session?.role ?? "Reader", msg);

  useEffect(() => {
    localStorage.setItem("pythonWiki.settings", JSON.stringify(settings));
    document.documentElement.lang = settings.locale;
    document.documentElement.dir = "ltr";
  }, [settings]);

  useEffect(() => {
    if (session) localStorage.setItem("pythonWiki.session", JSON.stringify(session));
    else localStorage.removeItem("pythonWiki.session");
    if (session) setStatus("");
  }, [session]);

  async function run<T>(action: () => Promise<T>, success?: string) {
    try {
      const result = await action();
      if (success) setStatus(success);
      return result;
    } catch (error) {
      setStatus(`${msg("requestFailed")}: ${error instanceof Error ? error.message : String(error)}`);
      return undefined;
    }
  }

  async function loadWorkspace() {
    if (!session) return;
    const [profile, nextArticles, nextCourses] = await Promise.all([
      run(() => api.me()),
      run(() => api.articles()),
      run(() => api.courses())
    ]);
    const graphXml = await run(() => api.graph("graphml"));
    if (typeof graphXml === "string") setArticleEdges(parseGraphMlEdges(graphXml));
    if (profile && profile.role !== session.role) {
      if (session.refreshToken) {
        const refreshed = await run(() => api.refresh(session.refreshToken));
        if (refreshed) setSession(refreshed);
      } else {
        setSession({ ...session, role: profile.role });
      }
    }
    if (nextArticles) setArticles(nextArticles);
    if (nextCourses) setCourses(nextCourses);
    if (session.role === "Admin") {
      const [nextUsers, nextRoles] = await Promise.all([run(() => api.users()), run(() => api.roles())]);
      if (nextUsers) setUsers(nextUsers);
      if (nextRoles) setRoles(nextRoles);
    }
    setLoadedAt(new Date().toISOString());
  }

  useEffect(() => {
    void loadWorkspace();
    if (session?.role) setActive("dashboard");
  }, [session?.token, session?.role]);

  useEffect(() => {
    if (!navItems.some((item) => item.id === active)) {
      setActive("dashboard");
    }
  }, [active, navItems]);

  const sortedArticles = sortText(settings.locale, articles, (item) => item.title);
  const sortedCourses = sortText(settings.locale, courses, (item) => item.title);
  const sortedUsers = sortText(settings.locale, users, (item) => item.username);

  if (!session) {
    return (
      <LandingPage
        api={api}
        msg={msg}
        run={run}
        session={session}
        setSession={setSession}
        settings={settings}
        setSettings={setSettings}
      />
    );
  }

  return (
    <AppShell
      active={active}
      api={api}
      loadedAt={loadedAt}
      msg={msg}
      navItems={navItems}
      onNavigate={setActive}
      onRefresh={loadWorkspace}
      run={run}
      session={session}
      setSession={setSession}
      settings={settings}
      setSettings={setSettings}
      status={status}
    >
      {active === "dashboard" && (
        <DashboardPage articles={articles} courses={courses} msg={msg} role={session.role} users={users} />
      )}
      {active === "articles" && (
        <ArticlesPage
          api={api}
          articles={sortedArticles}
          edges={articleEdges}
          locale={settings.locale}
          msg={msg}
          onCompleted={loadWorkspace}
          run={run}
          selected={selectedArticle}
          setSelected={setSelectedArticle}
        />
      )}
      {active === "courses" && <CoursesPage articles={articles} courses={sortedCourses} locale={settings.locale} msg={msg} />}
      {active === "editor" &&
        (canEdit ? (
          <ArticleEditorPage
            api={api}
            articles={articles}
            canCreate={session.role === "Creator" || session.role === "Admin"}
            edges={articleEdges}
            msg={msg}
            reload={loadWorkspace}
            run={run}
          />
        ) : (
          <EmptyState title={msg("editorWorkspace")} text={msg("editorHint")} />
        ))}
      {active === "creator" &&
        (canCreateCourse ? (
          <CourseEditorPage
            api={api}
            articles={articles}
            courses={courses}
            msg={msg}
            onCourseCreated={(course) =>
              setCourses((current) => [course, ...current.filter((item) => item.id !== course.id)])
            }
            reload={loadWorkspace}
            run={run}
          />
        ) : (
          <EmptyState title={msg("creatorWorkspace")} text={msg("creatorHint")} />
        ))}
      {active === "admin" &&
        (isAdmin ? (
          <AdminPage
            api={api}
            msg={msg}
            reload={loadWorkspace}
            roles={roles}
            run={run}
            users={sortedUsers}
          />
        ) : (
          <EmptyState title={msg("adminWorkspace")} text={msg("adminOnly")} />
        ))}
      {active === "settings" && <SettingsPage msg={msg} settings={settings} setSettings={setSettings} />}
    </AppShell>
  );
}
