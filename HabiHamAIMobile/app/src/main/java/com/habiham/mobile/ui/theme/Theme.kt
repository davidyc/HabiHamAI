package com.habiham.mobile.ui.theme

import android.app.Activity
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Shapes
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.view.WindowCompat

private val HabiHamDarkColors = darkColorScheme(
    primary = HabiHamColors.Primary,
    onPrimary = Color.White,
    primaryContainer = HabiHamColors.PrimaryDeep,
    onPrimaryContainer = HabiHamColors.TextBright,
    secondary = HabiHamColors.Ghost,
    onSecondary = HabiHamColors.Text,
    secondaryContainer = HabiHamColors.SurfaceSoft,
    onSecondaryContainer = HabiHamColors.Text,
    tertiary = HabiHamColors.Link,
    onTertiary = Color.White,
    tertiaryContainer = Color(0xFF0D2847),
    onTertiaryContainer = HabiHamColors.Link,
    background = HabiHamColors.BgEnd,
    onBackground = HabiHamColors.Text,
    surface = HabiHamColors.Surface,
    onSurface = HabiHamColors.Text,
    surfaceVariant = HabiHamColors.SurfaceSoft,
    onSurfaceVariant = HabiHamColors.Muted,
    surfaceTint = HabiHamColors.Primary,
    outline = HabiHamColors.PanelBorder,
    outlineVariant = Color(0xFF484F58),
    error = HabiHamColors.Danger,
    onError = Color.White,
    errorContainer = HabiHamColors.DangerSoft,
    onErrorContainer = Color(0xFFFECACA),
    inverseSurface = HabiHamColors.TextBright,
    inverseOnSurface = HabiHamColors.BgStart,
    inversePrimary = HabiHamColors.PrimaryHover,
    scrim = Color(0xFF010409),
)

private val HabiHamShapes = Shapes(
    extraSmall = RoundedCornerShape(8.dp),
    small = RoundedCornerShape(10.dp),
    medium = RoundedCornerShape(14.dp),
    large = RoundedCornerShape(16.dp),
    extraLarge = RoundedCornerShape(18.dp),
)

private val HabiHamTypography = Typography(
    headlineMedium = TextStyle(
        fontWeight = FontWeight.Bold,
        fontSize = 29.sp,
        lineHeight = 34.sp,
        color = HabiHamColors.TextBright,
    ),
    headlineSmall = TextStyle(
        fontWeight = FontWeight.Bold,
        fontSize = 22.sp,
        lineHeight = 28.sp,
        color = HabiHamColors.TextBright,
    ),
    titleLarge = TextStyle(
        fontWeight = FontWeight.Bold,
        fontSize = 22.sp,
        lineHeight = 28.sp,
        color = HabiHamColors.TextBright,
    ),
    titleMedium = TextStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = 18.sp,
        lineHeight = 24.sp,
        color = HabiHamColors.TextBright,
    ),
    bodyLarge = TextStyle(
        fontSize = 16.sp,
        lineHeight = 22.sp,
        color = HabiHamColors.Text,
    ),
    bodyMedium = TextStyle(
        fontSize = 14.sp,
        lineHeight = 20.sp,
        color = HabiHamColors.Text,
    ),
    bodySmall = TextStyle(
        fontSize = 13.sp,
        lineHeight = 18.sp,
        color = HabiHamColors.Muted,
    ),
    labelLarge = TextStyle(
        fontWeight = FontWeight.SemiBold,
        fontSize = 14.sp,
        lineHeight = 18.sp,
        color = HabiHamColors.Text,
    ),
    labelSmall = TextStyle(
        fontSize = 12.sp,
        lineHeight = 16.sp,
        color = HabiHamColors.Muted,
    ),
)

@Composable
fun HabiHamTheme(content: @Composable () -> Unit) {
    val colorScheme = HabiHamDarkColors
    val view = LocalView.current
    if (!view.isInEditMode) {
        SideEffect {
            val window = (view.context as Activity).window
            window.statusBarColor = Color.Transparent.toArgb()
            window.navigationBarColor = HabiHamColors.BgEnd.toArgb()
            WindowCompat.getInsetsController(window, view).isAppearanceLightStatusBars = false
            WindowCompat.getInsetsController(window, view).isAppearanceLightNavigationBars = false
        }
    }

    MaterialTheme(
        colorScheme = colorScheme,
        shapes = HabiHamShapes,
        typography = HabiHamTypography,
        content = content,
    )
}
