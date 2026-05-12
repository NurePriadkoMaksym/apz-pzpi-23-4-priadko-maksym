package com.example.pythonwiki

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import com.example.pythonwiki.ui.PythonWikiApp
import com.example.pythonwiki.ui.theme.PythonwikiTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            PythonwikiTheme {
                PythonWikiApp()
            }
        }
    }
}
