package com.example.pythonwiki.ui

import com.example.pythonwiki.data.AppContent
import com.example.pythonwiki.data.AuthSession
import com.example.pythonwiki.data.LoginCredentials
import com.example.pythonwiki.data.ProgressUpdate

enum class AppTab(val label: String) {
    Home("Home"),
    Library("Library"),
    Courses("Courses"),
    Graph("Graph"),
    Profile("Profile")
}

data class AppUiState(
    val session: AuthSession? = null,
    val content: AppContent? = null,
    val isLoading: Boolean = false,
    val errorMessage: String? = null,
    val lastLogin: LoginCredentials = LoginCredentials(email = "", password = ""),
    val authMode: AuthMode = AuthMode.Login,
    val articleLoadingId: Int? = null,
    val progressUpdate: ProgressUpdate? = null
)

enum class AuthMode {
    Login,
    Register
}
