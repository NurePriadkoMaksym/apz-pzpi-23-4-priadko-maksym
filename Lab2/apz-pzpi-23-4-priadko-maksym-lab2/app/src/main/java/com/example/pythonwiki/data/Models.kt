package com.example.pythonwiki.data

data class UserProfile(
    val id: Int,
    val username: String,
    val email: String,
    val role: String,
    val xp: Int,
    val nextRole: String,
    val xpToNextRole: Int,
    val streakDays: Int,
    val completedArticles: Int
)

data class ArticleSummary(
    val id: Int,
    val title: String,
    val category: String,
    val excerpt: String,
    val content: String,
    val xpReward: Int,
    val xpRequired: Int,
    val estimatedMinutes: Int,
    val isLocked: Boolean
)

data class CourseSummary(
    val id: Int,
    val title: String,
    val description: String,
    val articleIds: List<Int>,
    val completedCount: Int,
    val totalCount: Int
)

data class GraphSummary(
    val nodes: Int,
    val edges: Int,
    val centralTopics: List<String>,
    val learningPaths: List<String>,
    val rawGraphMl: String,
    val articles: List<GraphArticleNode>,
    val connections: List<GraphConnection>
)

data class GraphArticleNode(
    val id: Int,
    val title: String,
    val isLocked: Boolean,
    val xpRequired: Int,
    val degree: Int
)

data class GraphConnection(
    val fromId: Int,
    val toId: Int
)

data class DashboardState(
    val user: UserProfile,
    val spotlight: ArticleSummary,
    val recommended: List<ArticleSummary>,
    val recentCourses: List<CourseSummary>,
    val graph: GraphSummary
)

data class AuthSession(
    val token: String,
    val refreshToken: String,
    val userId: Int,
    val username: String,
    val role: String
)

data class LoginCredentials(
    val email: String,
    val password: String
)

data class ProgressUpdate(
    val xpGained: Int,
    val newXp: Int,
    val previousRole: String,
    val newRole: String,
    val completedArticlesDelta: Int
)

data class ReadArticleResult(
    val article: ArticleSummary,
    val content: AppContent,
    val progress: ProgressUpdate
)

data class AppContent(
    val session: AuthSession,
    val dashboard: DashboardState,
    val articles: List<ArticleSummary>,
    val courses: List<CourseSummary>,
    val graph: GraphSummary
)
