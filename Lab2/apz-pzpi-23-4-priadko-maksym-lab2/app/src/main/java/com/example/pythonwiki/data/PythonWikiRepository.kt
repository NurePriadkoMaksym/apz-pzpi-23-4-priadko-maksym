package com.example.pythonwiki.data

import android.content.Context
import com.example.pythonwiki.BuildConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import org.xmlpull.v1.XmlPullParser
import org.xmlpull.v1.XmlPullParserFactory
import java.io.BufferedReader
import java.io.StringReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL

class PythonWikiRepository(context: Context) {
    private val appContext = context.applicationContext
    private val prefs = appContext.getSharedPreferences("pythonwiki_session", Context.MODE_PRIVATE)

    suspend fun restoreSession(): AuthSession? = withContext(Dispatchers.IO) {
        val token = prefs.getString("token", null) ?: return@withContext null
        val refreshToken = prefs.getString("refresh_token", "") ?: ""
        val userId = prefs.getInt("user_id", -1)
        val username = prefs.getString("username", null) ?: return@withContext null
        val role = prefs.getString("role", "Reader") ?: "Reader"

        AuthSession(
            token = token,
            refreshToken = refreshToken,
            userId = userId,
            username = username,
            role = role
        )
    }

    suspend fun restoreLastLogin(): LoginCredentials = withContext(Dispatchers.IO) {
        LoginCredentials(
            email = prefs.getString("last_email", "").orEmpty(),
            password = prefs.getString("last_password", "").orEmpty()
        )
    }

    suspend fun login(email: String, password: String): AuthSession {
        val payload = JSONObject()
            .put("email", email)
            .put("password", password)

        val json = JSONObject(request(path = "Auth/login", method = "POST", body = payload))
        val session = AuthSession(
            token = json.getString("token"),
            refreshToken = json.optString("refreshToken"),
            userId = json.getInt("userId"),
            username = json.getString("username"),
            role = json.getString("role")
        )

        persistSession(session)
        persistLastLogin(email, password)
        return session
    }

    suspend fun register(username: String, email: String, password: String): AuthSession {
        val payload = JSONObject()
            .put("username", username)
            .put("email", email)
            .put("password", password)

        val json = JSONObject(request(path = "Auth/register", method = "POST", body = payload))
        val session = AuthSession(
            token = json.getString("token"),
            refreshToken = json.optString("refreshToken"),
            userId = json.getInt("userId"),
            username = json.getString("username"),
            role = json.getString("role")
        )

        persistSession(session)
        persistLastLogin(email, password)
        return session
    }

    suspend fun logout() = withContext(Dispatchers.IO) {
        prefs.edit()
            .remove("token")
            .remove("refresh_token")
            .remove("user_id")
            .remove("username")
            .remove("role")
            .apply()
    }

    suspend fun loadAppContent(session: AuthSession): AppContent {
        val user = fetchProfile(session)
        val articles = fetchArticles(session)
        val courses = runCatching { fetchCourses(session) }.getOrElse { emptyList() }
        val graph = fetchGraph(session, articles)
        val recommended = articles.sortedBy { it.xpRequired }.take(4)
        val spotlight = articles.maxByOrNull { it.xpReward } ?: throw ApiException("No articles returned from backend.")

        return AppContent(
            session = session,
            dashboard = DashboardState(
                user = user,
                spotlight = spotlight,
                recommended = recommended,
                recentCourses = courses.take(3),
                graph = graph
            ),
            articles = articles,
            courses = courses,
            graph = graph
        )
    }

    suspend fun readArticle(
        session: AuthSession,
        previousContent: AppContent,
        articleId: Int
    ): ReadArticleResult {
        val previousUser = previousContent.dashboard.user
        val json = JSONObject(request(path = "Article/$articleId", token = session.token))
        val article = mapArticle(json)
        val refreshedContent = loadAppContent(session)
        val refreshedUser = refreshedContent.dashboard.user

        return ReadArticleResult(
            article = article,
            content = refreshedContent,
            progress = ProgressUpdate(
                xpGained = (refreshedUser.xp - previousUser.xp).coerceAtLeast(0),
                newXp = refreshedUser.xp,
                previousRole = previousUser.role,
                newRole = refreshedUser.role,
                completedArticlesDelta = (refreshedUser.completedArticles - previousUser.completedArticles).coerceAtLeast(0)
            )
        )
    }

    fun serverBaseUrl(): String = BuildConfig.API_BASE_URL

    private suspend fun fetchProfile(session: AuthSession): UserProfile {
        val json = JSONObject(request(path = "User/me", token = session.token))
        val xp = json.getInt("xp")
        return UserProfile(
            id = json.getInt("id"),
            username = json.getString("username"),
            email = json.getString("email"),
            role = json.getString("role"),
            xp = xp,
            nextRole = nextRoleForXp(xp, json.getString("role")),
            xpToNextRole = xpToNextRole(xp, json.getString("role")),
            streakDays = 0,
            completedArticles = json.optInt("completedArticles", 0)
        )
    }

    private suspend fun fetchArticles(session: AuthSession): List<ArticleSummary> {
        val array = JSONArray(request(path = "Article/available", token = session.token))
        return buildList {
            for (index in 0 until array.length()) {
                add(mapArticle(array.getJSONObject(index)))
            }
        }
    }

    private suspend fun fetchCourses(session: AuthSession): List<CourseSummary> {
        val array = JSONArray(request(path = "courses", token = session.token))
        return buildList {
            for (index in 0 until array.length()) {
                val json = array.getJSONObject(index)
                val courseArticles = json.optJSONArray("courseArticles") ?: JSONArray()
                val articleIds = buildList {
                    for (articleIndex in 0 until courseArticles.length()) {
                        add(courseArticles.getJSONObject(articleIndex).getInt("articleId"))
                    }
                }
                add(
                    CourseSummary(
                        id = json.getInt("id"),
                        title = json.getString("title"),
                        description = json.optString("description"),
                        articleIds = articleIds,
                        completedCount = 0,
                        totalCount = articleIds.size
                    )
                )
            }
        }
    }

    private suspend fun fetchGraph(session: AuthSession, articles: List<ArticleSummary>): GraphSummary {
        val graphMl = request(path = "graph/graphml", token = session.token)
        val parsedGraph = parseGraphMl(graphMl)
        val nodes = parsedGraph.nodeIds
        val articleMap = articles.associateBy { it.id }
        val connections = parsedGraph.connections
        val degreeMap = connections
            .flatMap { listOf(it.fromId, it.toId) }
            .groupingBy { it }
            .eachCount()
        val pathLabels = connections.take(3).mapNotNull { connection ->
            val from = articleMap[connection.fromId]?.title ?: return@mapNotNull null
            val to = articleMap[connection.toId]?.title ?: return@mapNotNull null
            "$from -> $to"
        }

        val centralTopics = nodes
            .sortedByDescending { degreeMap[it] ?: 0 }
            .take(3)
            .mapNotNull { articleMap[it]?.title }
        val graphArticles = nodes.mapNotNull { id ->
            val article = articleMap[id] ?: return@mapNotNull null
            GraphArticleNode(
                id = id,
                title = article.title,
                isLocked = article.isLocked,
                xpRequired = article.xpRequired,
                degree = degreeMap[id] ?: 0
            )
        }

        return GraphSummary(
            nodes = nodes.size,
            edges = connections.size,
            centralTopics = centralTopics,
            learningPaths = pathLabels,
            rawGraphMl = graphMl,
            articles = graphArticles,
            connections = connections
        )
    }

    private fun parseGraphMl(graphMl: String): ParsedGraph {
        val parser = XmlPullParserFactory.newInstance().newPullParser().apply {
            setFeature(XmlPullParser.FEATURE_PROCESS_NAMESPACES, true)
            setInput(StringReader(graphMl))
        }
        val nodeIds = mutableListOf<Int>()
        val connections = mutableListOf<GraphConnection>()

        while (parser.eventType != XmlPullParser.END_DOCUMENT) {
            if (parser.eventType == XmlPullParser.START_TAG) {
                when (parser.name) {
                    "node" -> parser.getAttributeValue(null, "id")
                        ?.toIntOrNull()
                        ?.let(nodeIds::add)
                    "edge" -> {
                        val fromId = parser.getAttributeValue(null, "source")?.toIntOrNull()
                        val toId = parser.getAttributeValue(null, "target")?.toIntOrNull()
                        if (fromId != null && toId != null) {
                            connections += GraphConnection(fromId = fromId, toId = toId)
                        }
                    }
                }
            }
            parser.next()
        }

        return ParsedGraph(
            nodeIds = nodeIds.distinct(),
            connections = connections
        )
    }

    private data class ParsedGraph(
        val nodeIds: List<Int>,
        val connections: List<GraphConnection>
    )

    private suspend fun request(
        path: String,
        method: String = "GET",
        token: String? = null,
        body: JSONObject? = null
    ): String = withContext(Dispatchers.IO) {
        val endpoint = URL(serverBaseUrl() + path)
        val connection = (endpoint.openConnection() as HttpURLConnection).apply {
            requestMethod = method
            connectTimeout = 15_000
            readTimeout = 15_000
            doInput = true
            setRequestProperty("Accept", "application/json")
            token?.let { setRequestProperty("Authorization", "Bearer $it") }
            if (body != null) {
                doOutput = true
                setRequestProperty("Content-Type", "application/json")
            }
        }

        try {
            if (body != null) {
                connection.outputStream.bufferedWriter().use { writer ->
                    writer.write(body.toString())
                }
            }

            val status = connection.responseCode
            val stream = if (status in 200..299) connection.inputStream else connection.errorStream
            val response = stream?.let { input ->
                BufferedReader(InputStreamReader(input)).use { reader -> reader.readText() }
            }.orEmpty()

            if (status !in 200..299) {
                throw ApiException(parseError(response).ifBlank { "Request failed with HTTP $status." })
            }

            response
        } catch (exception: Exception) {
            if (exception is ApiException) throw exception
            throw ApiException("Unable to reach PythonWiki backend at ${serverBaseUrl()}. ${exception.message.orEmpty()}".trim())
        } finally {
            connection.disconnect()
        }
    }

    private fun persistSession(session: AuthSession) {
        prefs.edit()
            .putString("token", session.token)
            .putString("refresh_token", session.refreshToken)
            .putInt("user_id", session.userId)
            .putString("username", session.username)
            .putString("role", session.role)
            .apply()
    }

    private fun persistLastLogin(email: String, password: String) {
        prefs.edit()
            .putString("last_email", email)
            .putString("last_password", password)
            .apply()
    }

    private fun parseError(response: String): String {
        if (response.isBlank()) return ""
        return runCatching { JSONObject(response).optString("error") }
            .getOrNull()
            .orEmpty()
            .ifBlank { response }
    }

    private fun inferCategory(title: String): String =
        title.substringBefore(" ").ifBlank { "Article" }

    private fun mapArticle(json: JSONObject): ArticleSummary {
        val title = json.getString("title")
        val content = json.optString("content")
        return ArticleSummary(
            id = json.getInt("id"),
            title = title,
            category = inferCategory(title),
            excerpt = content.take(140).ifBlank { "No summary available." },
            content = content,
            xpReward = json.getInt("xpReward"),
            xpRequired = json.getInt("xpRequired"),
            estimatedMinutes = estimateReadMinutes(content),
            isLocked = json.optBoolean("isLocked", false)
        )
    }

    private fun estimateReadMinutes(content: String): Int =
        (content.split(Regex("\\s+")).count { it.isNotBlank() } / 180).coerceAtLeast(1)

    private fun nextRoleForXp(xp: Int, currentRole: String): String = when {
        currentRole == "Admin" -> "Admin"
        xp < 500 -> "Editor"
        xp < 1500 -> "Creator"
        xp < 3000 -> "Moderator"
        else -> "Maxed"
    }

    private fun xpToNextRole(xp: Int, currentRole: String): Int = when {
        currentRole == "Admin" -> 0
        xp < 500 -> 500 - xp
        xp < 1500 -> 1500 - xp
        xp < 3000 -> 3000 - xp
        else -> 0
    }
}

class ApiException(message: String) : Exception(message)
