package com.example.pythonwiki.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import com.example.pythonwiki.data.ArticleSummary
import com.example.pythonwiki.data.CourseSummary
import com.example.pythonwiki.data.DashboardState
import com.example.pythonwiki.data.GraphSummary
import com.example.pythonwiki.ui.components.ArticleCard
import com.example.pythonwiki.ui.components.CourseCard
import com.example.pythonwiki.ui.components.HeroCard
import com.example.pythonwiki.ui.components.InfoPanel
import com.example.pythonwiki.ui.components.SectionTitle
import com.example.pythonwiki.ui.components.StatStrip
import com.example.pythonwiki.ui.graph.KnowledgeMap

@Composable
fun HomeScreen(
    dashboard: DashboardState,
    onOpenArticle: (ArticleSummary) -> Unit,
    onRefresh: () -> Unit,
    errorMessage: String?
) {
    LazyColumn(
        modifier = Modifier
            .fillMaxSize()
            .background(MaterialTheme.colorScheme.background),
        contentPadding = PaddingValues(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            HeroCard(
                title = "PythonWiki",
                subtitle = "Track your progress and continue learning.",
                eyebrow = "Dashboard"
            )
        }
        item {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                StatStrip(
                    items = listOf(
                        "${dashboard.user.xp} XP",
                        dashboard.user.role,
                        "${dashboard.user.completedArticles} completed"
                    )
                )
                Text(
                    text = "Refresh",
                    modifier = Modifier.clickable(onClick = onRefresh),
                    color = MaterialTheme.colorScheme.tertiary
                )
            }
        }
        if (!errorMessage.isNullOrBlank()) {
            item { Text(errorMessage, color = MaterialTheme.colorScheme.error) }
        }
        item { SectionTitle("Spotlight") }
        item { ArticleCard(article = dashboard.spotlight, onClick = { onOpenArticle(dashboard.spotlight) }) }
        item { SectionTitle("Recommended Reads") }
        items(dashboard.recommended) { article ->
            ArticleCard(article = article, onClick = { onOpenArticle(article) })
        }
        item { SectionTitle("Course Access") }
        items(dashboard.recentCourses) { course ->
            CourseCard(course = course)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun LibraryScreen(
    articles: List<ArticleSummary>,
    onOpenArticle: (ArticleSummary) -> Unit
) {
    var query by rememberSaveable { mutableStateOf("") }
    var activeFilter by rememberSaveable { mutableStateOf("All") }

    val filters = listOf("All", "Unlocked", "Locked")
    val filtered = articles.filter { article ->
        val matchesQuery = query.isBlank() ||
            article.title.contains(query, ignoreCase = true) ||
            article.category.contains(query, ignoreCase = true)
        val matchesFilter = when (activeFilter) {
            "Unlocked" -> !article.isLocked
            "Locked" -> article.isLocked
            else -> true
        }
        matchesQuery && matchesFilter
    }

    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            SectionTitle("Library")
            Text(
                text = "Browse available articles.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        item {
            OutlinedTextField(
                value = query,
                onValueChange = { query = it },
                modifier = Modifier.fillMaxWidth(),
                label = { Text("Search titles or categories") },
                singleLine = true
            )
        }
        item {
            FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                filters.forEach { filter ->
                    FilterChip(
                        selected = activeFilter == filter,
                        onClick = { activeFilter = filter },
                        label = { Text(filter) }
                    )
                }
            }
        }
        items(filtered) { article ->
            ArticleCard(article = article, onClick = { onOpenArticle(article) })
        }
    }
}

@Composable
fun CoursesScreen(
    courses: List<CourseSummary>,
    articles: List<ArticleSummary>,
    onOpenArticle: (ArticleSummary) -> Unit
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            SectionTitle("Courses")
            Text(
                text = "Follow guided learning paths.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        items(courses) { course ->
            Card(shape = RoundedCornerShape(24.dp)) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(18.dp),
                    verticalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    Text(course.title, style = MaterialTheme.typography.titleLarge)
                    Text(
                        text = course.description,
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    StatStrip(items = listOf("${course.totalCount} articles", "0 tracked complete"))
                    HorizontalDivider()
                    course.articleIds.forEach { articleId ->
                        val article = articles.firstOrNull { it.id == articleId } ?: return@forEach
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clip(RoundedCornerShape(16.dp))
                                .clickable { onOpenArticle(article) }
                                .padding(vertical = 10.dp),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Column(modifier = Modifier.weight(1f)) {
                                Text(article.title, style = MaterialTheme.typography.titleMedium)
                                Text(
                                    text = article.category,
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            Text("${article.estimatedMinutes} min", style = MaterialTheme.typography.labelLarge)
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun GraphScreen(
    graph: GraphSummary,
    articles: List<ArticleSummary>,
    onOpenArticle: (ArticleSummary) -> Unit
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            SectionTitle("Knowledge Graph")
            Text(
                text = "Explore how topics connect.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        item { StatStrip(items = listOf("${graph.nodes} nodes", "${graph.edges} edges")) }
        item { KnowledgeMap(graph = graph, articles = articles, onOpenArticle = onOpenArticle) }
        item { SectionTitle("Central Topics") }
        item {
            FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                graph.centralTopics.forEach { topic ->
                    AssistChip(onClick = {}, label = { Text(topic) })
                }
            }
        }
        item { SectionTitle("Path Suggestions") }
        items(graph.learningPaths) { path ->
            Card(shape = RoundedCornerShape(20.dp)) {
                Text(
                    text = path,
                    modifier = Modifier.padding(16.dp),
                    style = MaterialTheme.typography.bodyLarge
                )
            }
        }
    }
}

@Composable
fun ProfileScreen(
    username: String,
    email: String,
    role: String,
    xp: Int,
    xpToNextRole: Int,
    nextRole: String,
    completedArticles: Int,
    onRefresh: () -> Unit,
    onLogout: () -> Unit
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item { HeroCard(title = username, subtitle = email, eyebrow = role) }
        item {
            StatStrip(
                items = listOf(
                    "$xp XP",
                    "$completedArticles completed",
                    if (xpToNextRole > 0) "$xpToNextRole XP to $nextRole" else "Top rank"
                )
            )
        }
        item { InfoPanel(title = "Account", lines = listOf(email, role)) }
        item {
            Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                Button(onClick = onRefresh) { Text("Refresh") }
                Button(onClick = onLogout) { Text("Log Out") }
            }
        }
    }
}

@Composable
fun ArticleDetailScreen(
    article: ArticleSummary,
    isLoading: Boolean,
    progressMessage: String?,
    onDismissProgress: () -> Unit,
    onBack: () -> Unit
) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(20.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        item {
            Text(
                text = "Back",
                modifier = Modifier
                    .clip(RoundedCornerShape(12.dp))
                    .clickable { onBack() }
                    .padding(vertical = 8.dp),
                color = MaterialTheme.colorScheme.tertiary,
                style = MaterialTheme.typography.labelLarge
            )
        }
        item {
            Text(article.category.uppercase(), color = MaterialTheme.colorScheme.tertiary)
            Spacer(modifier = Modifier.height(6.dp))
            Text(article.title, style = MaterialTheme.typography.headlineMedium)
            Spacer(modifier = Modifier.height(10.dp))
            StatStrip(
                items = listOf(
                    "${article.xpReward} XP reward",
                    "${article.xpRequired} XP required",
                    "${article.estimatedMinutes} min"
                )
            )
        }
        if (isLoading) {
            item {
                InfoPanel(
                    title = "Updating Progress",
                    lines = listOf("Syncing your progress and XP with the server.")
                )
            }
        }
        if (!progressMessage.isNullOrBlank()) {
            item {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(20.dp))
                        .background(MaterialTheme.colorScheme.surfaceVariant)
                        .padding(16.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = progressMessage,
                        modifier = Modifier.weight(1f),
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    TextButton(onClick = onDismissProgress) {
                        Text("Dismiss")
                    }
                }
            }
        }
        item { Text(article.content, style = MaterialTheme.typography.bodyLarge) }
    }
}
