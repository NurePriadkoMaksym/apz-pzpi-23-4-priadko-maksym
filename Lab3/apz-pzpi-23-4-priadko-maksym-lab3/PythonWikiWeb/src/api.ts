import { AdminUser, Article, AuthResponse, Course, Role, UserProfile } from "./types";

export class ApiClient {
  constructor(private getBaseUrl: () => string, private getToken: () => string | null) {}

  async request<T>(path: string, init: RequestInit = {}, raw = false): Promise<T> {
    const headers = new Headers(init.headers);
    const token = this.getToken();
    if (!raw && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
    if (token) headers.set("Authorization", `Bearer ${token}`);

    const baseUrl = this.getBaseUrl().replace(/\/$/, "");
    const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
    if (!response.ok) {
      const message = await response.text();
      throw new Error(formatApiError(message) || `${response.status} ${response.statusText}`);
    }
    if (response.status === 204) return undefined as T;
    const contentType = response.headers.get("content-type") || "";
    if (contentType.includes("application/json")) return response.json() as Promise<T>;
    if (contentType.includes("image/png")) return response.blob() as Promise<T>;
    return response.text() as Promise<T>;
  }

  login(email: string, password: string) {
    return this.request<AuthResponse>("/api/Auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password })
    });
  }

  register(username: string, email: string, password: string) {
    return this.request<AuthResponse>("/api/Auth/register", {
      method: "POST",
      body: JSON.stringify({ username, email, password })
    });
  }

  logout(refreshToken?: string) {
    return this.request<void>("/api/Auth/logout", {
      method: "POST",
      body: JSON.stringify({ refreshToken })
    });
  }

  refresh(refreshToken: string) {
    return this.request<AuthResponse>("/api/Auth/refresh", {
      method: "POST",
      body: JSON.stringify({ refreshToken })
    });
  }

  me() {
    return this.request<UserProfile>("/api/User/me");
  }

  users() {
    return this.request<AdminUser[]>("/api/Admin/users");
  }

  roles() {
    return this.request<Role[]>("/api/Admin/roles");
  }

  changeRole(userId: number, roleName: Role) {
    return this.request("/api/Admin/change-role", {
      method: "POST",
      body: JSON.stringify({ userId, roleName })
    });
  }

  deleteUser(userId: number) {
    return this.request<void>(`/api/User/${userId}`, { method: "DELETE" });
  }

  articles() {
    return this.request<Article[]>("/api/Article/available");
  }

  article(id: number) {
    return this.request<Article>(`/api/Article/${id}`);
  }

  searchArticle(keyword: string) {
    return this.request<Article>(`/api/Article/search?keyword=${encodeURIComponent(keyword)}`);
  }

  createArticle(input: Omit<Article, "id" | "isLocked" | "authorId"> & { linkToArticleIds?: number[] }) {
    return this.request<Article>("/api/Article", { method: "POST", body: JSON.stringify(input) });
  }

  updateArticle(id: number, input: Omit<Article, "id" | "authorId">) {
    return this.request<Article>(`/api/Article/${id}`, { method: "PUT", body: JSON.stringify(input) });
  }

  deleteArticle(id: number) {
    return this.request<void>(`/api/Article/${id}`, { method: "DELETE" });
  }

  link(fromArticleId: number, toArticleId: number) {
    return this.request("/api/Article/link", {
      method: "POST",
      body: JSON.stringify({ fromArticleId, toArticleId })
    });
  }

  unlink(fromArticleId: number, toArticleId: number) {
    return this.request("/api/Article/link", {
      method: "DELETE",
      body: JSON.stringify({ fromArticleId, toArticleId })
    });
  }

  courses() {
    return this.request<Course[]>("/api/courses");
  }

  createCourse(input: Omit<Course, "id">) {
    return this.request<Course>("/api/courses", { method: "POST", body: JSON.stringify(input) });
  }

  deleteCourse(id: number) {
    return this.request<void>(`/api/courses/${id}`, { method: "DELETE" });
  }

  completeArticle(articleId: number) {
    return this.request<{ message: string; xp: number; level: number }>(`/api/xp/complete/${articleId}`, {
      method: "POST"
    });
  }

  graph(format: "dot" | "graphml" | "png") {
    return this.request<string | Blob>(`/api/graph/${format}`, {}, true);
  }
}

function formatApiError(message: string) {
  if (!message) return "";
  try {
    const parsed = JSON.parse(message) as { error?: string; title?: string; errors?: Record<string, string[]> };
    if (parsed.error) return parsed.error;
    if (parsed.errors) return Object.values(parsed.errors).flat().join(" ");
    if (parsed.title) return parsed.title;
  } catch {
    return message;
  }
  return message;
}
