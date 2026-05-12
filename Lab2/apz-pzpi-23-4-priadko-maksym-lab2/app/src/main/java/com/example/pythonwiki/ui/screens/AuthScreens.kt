package com.example.pythonwiki.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import com.example.pythonwiki.ui.AuthMode

@Composable
fun LoginScreen(
    mode: AuthMode,
    isLoading: Boolean,
    errorMessage: String?,
    initialEmail: String,
    initialPassword: String,
    onLogin: (String, String) -> Unit,
    onRegister: (String, String, String) -> Unit,
    onModeChange: (AuthMode) -> Unit
) {
    var username by rememberSaveable { mutableStateOf("") }
    var email by rememberSaveable(initialEmail) { mutableStateOf(initialEmail) }
    var password by rememberSaveable(initialPassword) { mutableStateOf(initialPassword) }

    val isRegister = mode == AuthMode.Register

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(MaterialTheme.colorScheme.background)
            .padding(24.dp),
        contentAlignment = Alignment.Center
    ) {
        Card(shape = RoundedCornerShape(28.dp)) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(24.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                Text("PythonWiki", style = MaterialTheme.typography.headlineLarge)
                Text(
                    text = if (isRegister) "Create an account to start learning." else "Sign in to continue.",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                    AuthModeToggle("Log In", !isRegister) { onModeChange(AuthMode.Login) }
                    AuthModeToggle("Register", isRegister) { onModeChange(AuthMode.Register) }
                }
                if (isRegister) {
                    OutlinedTextField(
                        value = username,
                        onValueChange = { username = it },
                        label = { Text("Username") },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true
                    )
                }
                OutlinedTextField(
                    value = email,
                    onValueChange = { email = it },
                    label = { Text("Email") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true
                )
                OutlinedTextField(
                    value = password,
                    onValueChange = { password = it },
                    label = { Text("Password") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    visualTransformation = PasswordVisualTransformation()
                )
                if (!errorMessage.isNullOrBlank()) {
                    Text(errorMessage, color = MaterialTheme.colorScheme.error)
                }
                Button(
                    onClick = {
                        if (isRegister) onRegister(username.trim(), email.trim(), password)
                        else onLogin(email.trim(), password)
                    },
                    enabled = !isLoading && email.isNotBlank() && password.isNotBlank() && (!isRegister || username.isNotBlank()),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    if (isLoading) {
                        CircularProgressIndicator(modifier = Modifier.width(18.dp), strokeWidth = 2.dp)
                    } else {
                        Text(if (isRegister) "Create Account" else "Log In")
                    }
                }
            }
        }
    }
}

@Composable
private fun AuthModeToggle(label: String, selected: Boolean, onClick: () -> Unit) {
    Text(
        text = label,
        modifier = Modifier
            .clickable(onClick = onClick)
            .background(
                color = if (selected) MaterialTheme.colorScheme.surfaceVariant else MaterialTheme.colorScheme.background,
                shape = RoundedCornerShape(50)
            )
            .padding(horizontal = 12.dp, vertical = 8.dp),
        color = if (selected) MaterialTheme.colorScheme.onSurface else MaterialTheme.colorScheme.onSurfaceVariant
    )
}

@Composable
fun LoadingScreen() {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        CircularProgressIndicator()
    }
}

@Composable
fun ErrorScreen(message: String, onRetry: () -> Unit, onLogout: () -> Unit) {
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Column(
            modifier = Modifier.padding(24.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text("Could not load PythonWiki", style = MaterialTheme.typography.headlineMedium)
            Text(message, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Button(onClick = onRetry) { Text("Retry") }
            Text(
                text = "Log out",
                modifier = Modifier.clickable(onClick = onLogout),
                color = MaterialTheme.colorScheme.tertiary
            )
        }
    }
}
