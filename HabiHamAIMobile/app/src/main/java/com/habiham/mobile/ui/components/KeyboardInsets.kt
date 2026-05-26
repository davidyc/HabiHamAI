package com.habiham.mobile.ui.components

import android.view.WindowManager
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.WindowInsetsSides
import androidx.compose.foundation.layout.imeNestedScroll
import androidx.compose.foundation.layout.only
import androidx.compose.foundation.layout.safeDrawing
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.window.DialogWindowProvider

/** Шапка Scaffold не сжимается при появлении клавиатуры — двигается только контент. */
object HabiHamKeyboardInsets {
    val scaffoldContent: WindowInsets
        @OptIn(ExperimentalLayoutApi::class)
        @Composable
        get() = WindowInsets.safeDrawing.only(
            WindowInsetsSides.Horizontal + WindowInsetsSides.Top,
        )
}

/** Прокрутка + подъём контента над клавиатурой без сдвига всего окна. */
@OptIn(ExperimentalLayoutApi::class)
fun Modifier.scrollWithIme(): Modifier = imeNestedScroll()

@Composable
fun DialogImeAdjustResize() {
    val view = LocalView.current
    SideEffect {
        val window = (view.parent as? DialogWindowProvider)?.window
        window?.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE)
    }
}
