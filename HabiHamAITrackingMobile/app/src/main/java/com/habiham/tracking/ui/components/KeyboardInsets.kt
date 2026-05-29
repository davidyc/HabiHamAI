package com.habiham.tracking.ui.components

import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.WindowInsetsSides
import androidx.compose.foundation.layout.imeNestedScroll
import androidx.compose.foundation.layout.only
import androidx.compose.foundation.layout.safeDrawing
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

object HabiHamKeyboardInsets {
    val scaffoldContent: WindowInsets
        @OptIn(ExperimentalLayoutApi::class)
        @Composable
        get() = WindowInsets.safeDrawing.only(
            WindowInsetsSides.Horizontal + WindowInsetsSides.Top,
        )
}

@OptIn(ExperimentalLayoutApi::class)
fun Modifier.scrollWithIme(): Modifier = imeNestedScroll()
