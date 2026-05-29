package com.habiham.mobile.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import com.habiham.mobile.ui.theme.HabiHamColors

private val pageBackgroundBrush = Brush.linearGradient(
    colors = listOf(HabiHamColors.BgStart, HabiHamColors.BgEnd),
    start = Offset(0f, 0f),
    end = Offset(1200f, 1800f),
)

private val pageGlowBrush = Brush.radialGradient(
    colors = listOf(
        HabiHamColors.Primary.copy(alpha = 0.08f),
        HabiHamColors.Primary.copy(alpha = 0f),
    ),
    center = Offset(900f, 0f),
    radius = 900f,
)

@Composable
fun HabiHamScreen(
    modifier: Modifier = Modifier,
    content: @Composable BoxScope.() -> Unit,
) {
    Box(
        modifier = modifier
            .fillMaxSize()
            .background(pageBackgroundBrush)
            .background(pageGlowBrush),
        content = content,
    )
}
