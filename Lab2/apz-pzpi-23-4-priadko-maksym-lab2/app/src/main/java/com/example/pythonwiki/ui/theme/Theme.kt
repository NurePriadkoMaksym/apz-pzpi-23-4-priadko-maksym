package com.example.pythonwiki.ui.theme

import androidx.compose.ui.graphics.Color
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val DarkColorScheme = darkColorScheme(
    primary = Foam,
    onPrimary = Ink,
    secondary = Mist,
    onSecondary = Ink,
    tertiary = Signal,
    background = Ink,
    onBackground = Paper,
    surface = River,
    onSurface = Paper,
    surfaceVariant = Steel,
    onSurfaceVariant = Foam
)

private val LightColorScheme = lightColorScheme(
    primary = River,
    onPrimary = Paper,
    secondary = Steel,
    onSecondary = Paper,
    tertiary = Signal,
    background = Paper,
    onBackground = Ink,
    surface = Color.White,
    onSurface = Ink,
    surfaceVariant = Foam,
    onSurfaceVariant = River,
    outline = Mist
)

@Composable
fun PythonwikiTheme(
    darkTheme: Boolean = false,
    content: @Composable () -> Unit
) {
    val colorScheme = if (darkTheme) DarkColorScheme else LightColorScheme

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography,
        content = content
    )
}
