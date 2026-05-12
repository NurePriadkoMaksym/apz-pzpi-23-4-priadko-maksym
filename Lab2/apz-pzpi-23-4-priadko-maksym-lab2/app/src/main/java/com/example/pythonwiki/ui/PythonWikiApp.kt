package com.example.pythonwiki.ui

import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import com.example.pythonwiki.data.AppContent
import com.example.pythonwiki.data.ArticleSummary
import com.example.pythonwiki.data.AuthSession
import com.example.pythonwiki.data.ProgressUpdate
import com.example.pythonwiki.data.PythonWikiRepository
import com.example.pythonwiki.data.ReadArticleResult
import com.example.pythonwiki.ui.screens.ArticleDetailScreen
import com.example.pythonwiki.ui.screens.CoursesScreen
import com.example.pythonwiki.ui.screens.ErrorScreen
import com.example.pythonwiki.ui.screens.GraphScreen
import com.example.pythonwiki.ui.screens.HomeScreen
import com.example.pythonwiki.ui.screens.LibraryScreen
import com.example.pythonwiki.ui.screens.LoadingScreen
import com.example.pythonwiki.ui.screens.LoginScreen
import com.example.pythonwiki.ui.screens.ProfileScreen
import kotlinx.coroutines.launch

@Composable
fun PythonWikiApp() {
    val context = LocalContext.current.applicationContext
    val repository = remember(context) { PythonWikiRepository(context) }
    val scope = rememberCoroutineScope()

    var uiState by remember { mutableStateOf(AppUiState(isLoading = true)) }
    var activeTab by rememberSaveable { mutableStateOf(AppTab.Home) }
    var selectedArticleId by rememberSaveable { mutableStateOf<Int?>(null) }
    var selectedArticle by remember { mutableStateOf<ArticleSummary?>(null) }

    fun resetSessionState() {
        selectedArticleId = null
        selectedArticle = null
        activeTab = AppTab.Home
    }

    fun loadContent(session: AuthSession) {
        scope.launch {
            uiState = uiState.copy(isLoading = true, errorMessage = null)
            uiState = try {
                uiState.copy(
                    session = session,
                    content = repository.loadAppContent(session),
                    isLoading = false,
                    errorMessage = null
                )
            } catch (exception: Exception) {
                uiState.copy(
                    session = session,
                    content = null,
                    isLoading = false,
                    errorMessage = exception.message ?: "Unable to load PythonWiki data."
                )
            }
        }
    }

    fun completeAuth(
        email: String,
        password: String,
        authAction: suspend () -> AuthSession
    ) {
        scope.launch {
            uiState = uiState.copy(isLoading = true, errorMessage = null)
            uiState = try {
                uiState.copy(
                    session = authAction(),
                    lastLogin = uiState.lastLogin.copy(email = email, password = password)
                )
            } catch (exception: Exception) {
                uiState.copy(
                    isLoading = false,
                    errorMessage = exception.message ?: "Authentication failed."
                )
            }
            uiState.session?.let(::loadContent)
        }
    }

    fun openArticle(article: ArticleSummary) {
        val session = uiState.session ?: return
        val content = uiState.content ?: return

        selectedArticleId = article.id
        uiState = uiState.copy(articleLoadingId = article.id, errorMessage = null, progressUpdate = null)

        scope.launch {
            val result = runCatching { repository.readArticle(session, content, article.id) }

            uiState = result.fold(
                onSuccess = { readResult: ReadArticleResult ->
                    selectedArticle = readResult.article
                    uiState.copy(
                        content = readResult.content,
                        session = readResult.content.session,
                        articleLoadingId = null,
                        progressUpdate = readResult.progress,
                        errorMessage = null
                    )
                },
                onFailure = { exception ->
                    selectedArticle = article
                    uiState.copy(
                        articleLoadingId = null,
                        errorMessage = exception.message ?: "Could not open article."
                    )
                }
            )
        }
    }

    LaunchedEffect(Unit) {
        val lastLogin = repository.restoreLastLogin()
        val session = repository.restoreSession()
        if (session != null) {
            uiState = uiState.copy(lastLogin = lastLogin)
            loadContent(session)
        } else {
            uiState = uiState.copy(isLoading = false, lastLogin = lastLogin)
        }
    }

    Surface(modifier = Modifier.fillMaxSize()) {
        when {
            uiState.session == null -> LoginScreen(
                mode = uiState.authMode,
                isLoading = uiState.isLoading,
                errorMessage = uiState.errorMessage,
                initialEmail = uiState.lastLogin.email,
                initialPassword = uiState.lastLogin.password,
                onLogin = { email, password ->
                    completeAuth(email, password) { repository.login(email, password) }
                },
                onRegister = { username, email, password ->
                    completeAuth(email, password) { repository.register(username, email, password) }
                },
                onModeChange = { mode ->
                    uiState = uiState.copy(authMode = mode, errorMessage = null)
                }
            )

            uiState.isLoading && uiState.content == null -> LoadingScreen()
            uiState.content == null -> ErrorScreen(
                message = uiState.errorMessage ?: "Unable to load app data.",
                onRetry = { uiState.session?.let(::loadContent) },
                onLogout = {
                    scope.launch {
                        repository.logout()
                        resetSessionState()
                        uiState = AppUiState(
                            isLoading = false,
                            authMode = AuthMode.Login,
                            lastLogin = repository.restoreLastLogin()
                        )
                    }
                }
            )

            else -> {
                val content = uiState.content ?: return@Surface
                val fallbackArticle = content.articles.firstOrNull { it.id == selectedArticleId }
                AuthenticatedApp(
                    content = content,
                    activeTab = activeTab,
                    selectedArticle = selectedArticle ?: fallbackArticle,
                    articleLoading = uiState.articleLoadingId != null,
                    progressMessage = uiState.progressUpdate?.let(::formatProgressMessage),
                    onClearProgress = { uiState = uiState.copy(progressUpdate = null) },
                    onTabChange = { activeTab = it },
                    onOpenArticle = ::openArticle,
                    onCloseArticle = {
                        selectedArticleId = null
                        selectedArticle = null
                    },
                    onRefresh = { uiState.session?.let(::loadContent) },
                    onLogout = {
                        scope.launch {
                            repository.logout()
                            resetSessionState()
                            uiState = AppUiState(
                                isLoading = false,
                                authMode = AuthMode.Login,
                                lastLogin = repository.restoreLastLogin()
                            )
                        }
                    },
                    errorMessage = uiState.errorMessage
                )
            }
        }
    }
}

@Composable
private fun AuthenticatedApp(
    content: AppContent,
    activeTab: AppTab,
    selectedArticle: ArticleSummary?,
    articleLoading: Boolean,
    progressMessage: String?,
    onClearProgress: () -> Unit,
    onTabChange: (AppTab) -> Unit,
    onOpenArticle: (ArticleSummary) -> Unit,
    onCloseArticle: () -> Unit,
    onRefresh: () -> Unit,
    onLogout: () -> Unit,
    errorMessage: String?
) {
    Scaffold(
        bottomBar = {
            NavigationBar {
                AppTab.entries.forEach { tab ->
                    NavigationBarItem(
                        selected = activeTab == tab,
                        onClick = { onTabChange(tab) },
                        icon = { Text(tab.label.take(1), style = MaterialTheme.typography.labelLarge) },
                        label = { Text(tab.label) }
                    )
                }
            }
        }
    ) { innerPadding ->
        Surface(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            if (selectedArticle != null) {
                ArticleDetailScreen(
                    article = selectedArticle,
                    isLoading = articleLoading,
                    progressMessage = progressMessage,
                    onDismissProgress = onClearProgress,
                    onBack = onCloseArticle
                )
            } else {
                when (activeTab) {
                    AppTab.Home -> HomeScreen(
                        dashboard = content.dashboard,
                        onOpenArticle = onOpenArticle,
                        onRefresh = onRefresh,
                        errorMessage = errorMessage
                    )
                    AppTab.Library -> LibraryScreen(articles = content.articles, onOpenArticle = onOpenArticle)
                    AppTab.Courses -> CoursesScreen(
                        courses = content.courses,
                        articles = content.articles,
                        onOpenArticle = onOpenArticle
                    )
                    AppTab.Graph -> GraphScreen(
                        graph = content.graph,
                        articles = content.articles,
                        onOpenArticle = onOpenArticle
                    )
                    AppTab.Profile -> ProfileScreen(
                        username = content.dashboard.user.username,
                        email = content.dashboard.user.email,
                        role = content.dashboard.user.role,
                        xp = content.dashboard.user.xp,
                        xpToNextRole = content.dashboard.user.xpToNextRole,
                        nextRole = content.dashboard.user.nextRole,
                        completedArticles = content.dashboard.user.completedArticles,
                        onRefresh = onRefresh,
                        onLogout = onLogout
                    )
                }
            }
        }
    }
}

private fun formatProgressMessage(progress: ProgressUpdate): String {
    val parts = mutableListOf<String>()
    if (progress.xpGained > 0) {
        parts += "+${progress.xpGained} XP"
    }
    if (progress.completedArticlesDelta > 0) {
        parts += "article completed"
    }
    if (progress.newRole != progress.previousRole) {
        parts += "Level up: ${progress.previousRole} -> ${progress.newRole}"
    }
    return if (parts.isEmpty()) {
        "Progress updated."
    } else {
        parts.joinToString(" | ")
    }
}
