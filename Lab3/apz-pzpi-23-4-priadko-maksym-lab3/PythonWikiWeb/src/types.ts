export type Role = "Reader" | "Editor" | "Creator" | "Admin";
export type LocaleCode = "uk-UA" | "en-US";

export interface AuthResponse {
  token: string;
  expiresAt: string;
  refreshToken: string;
  refreshExpiresAt?: string;
  refreshTokenExpiresAt?: string;
  userId: number;
  username: string;
  role: Role;
}

export interface UserProfile {
  id: number;
  username: string;
  email: string;
  xp: number;
  role: Role;
  completedArticles: number;
}

export interface AdminUser {
  id: number;
  username: string;
  email: string;
  xp: number;
  role: Role;
}

export interface Article {
  id: number;
  title: string;
  content: string;
  xpReward: number;
  xpRequired: number;
  isLocked: boolean;
  authorId?: number | null;
}

export interface ArticleEdge {
  from: number;
  to: number;
}

export interface Course {
  id: number;
  title: string;
  description: string;
  xpRequired: number;
  articleIds: number[];
}

export interface Settings {
  locale: LocaleCode;
  apiBaseUrl: string;
}
