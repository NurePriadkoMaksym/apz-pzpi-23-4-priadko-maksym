import { NavItem } from "./components/AppShell";
import { Role } from "./types";

export function getNavItems(role: Role, msg: (key: string) => string): NavItem[] {
  const reader: NavItem[] = [
    { id: "dashboard", label: msg("readerDashboard") },
    { id: "articles", label: msg("articles") },
    { id: "courses", label: msg("courses") }
  ];

  if (role === "Editor") {
    return [
      { id: "dashboard", label: msg("editorDashboard") },
      { id: "editor", label: msg("editorWorkspace") },
      { id: "articles", label: msg("articles") }
    ];
  }

  if (role === "Creator") {
    return [
      { id: "dashboard", label: msg("creatorDashboard") },
      { id: "creator", label: msg("creatorWorkspace") },
      { id: "editor", label: msg("editorWorkspace") },
      { id: "articles", label: msg("articles") },
      { id: "courses", label: msg("courses") }
    ];
  }

  if (role === "Admin") {
    return [
      { id: "dashboard", label: msg("adminDashboard") },
      { id: "admin", label: msg("adminWorkspace") },
      { id: "editor", label: msg("editorWorkspace") },
      { id: "creator", label: msg("creatorWorkspace") },
      { id: "articles", label: msg("articles") },
      { id: "settings", label: msg("settings") }
    ];
  }

  return reader;
}
