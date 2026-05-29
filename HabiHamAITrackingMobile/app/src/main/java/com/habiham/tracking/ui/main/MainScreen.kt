package com.habiham.tracking.ui.main

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.habiham.tracking.ui.components.HabiHamBrandTitle
import com.habiham.tracking.ui.components.HabiHamKeyboardInsets
import com.habiham.tracking.ui.components.HabiHamScreen
import com.habiham.tracking.ui.components.HabiHamSegmentTabRow
import com.habiham.tracking.ui.habits.HabitsTab
import com.habiham.tracking.ui.habits.HabitsViewModel
import com.habiham.tracking.ui.todos.TodosTab
import com.habiham.tracking.ui.todos.TodosViewModel

private enum class TrackingTab(val title: String) {
    Habits("Привычки"),
    Todos("Задачи"),
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainScreen(
    habitsViewModel: HabitsViewModel,
    todosViewModel: TodosViewModel,
    onOpenApiSettings: () -> Unit,
    onLogout: () -> Unit,
) {
    var selectedTab by rememberSaveable { mutableIntStateOf(0) }
    val tab = TrackingTab.entries[selectedTab]

    HabiHamScreen {
        Scaffold(
            modifier = Modifier.fillMaxSize(),
            containerColor = Color.Transparent,
            contentWindowInsets = HabiHamKeyboardInsets.scaffoldContent,
            topBar = {
                Column {
                    TopAppBar(
                        title = { HabiHamBrandTitle("HabiHam Трекинг") },
                        colors = TopAppBarDefaults.topAppBarColors(
                            containerColor = MaterialTheme.colorScheme.surfaceVariant,
                            titleContentColor = MaterialTheme.colorScheme.onSurface,
                            actionIconContentColor = MaterialTheme.colorScheme.onSurface,
                        ),
                        actions = {
                            IconButton(
                                onClick = {
                                    when (tab) {
                                        TrackingTab.Habits -> habitsViewModel.refresh()
                                        TrackingTab.Todos -> todosViewModel.refresh()
                                    }
                                },
                            ) {
                                Icon(Icons.Default.Refresh, contentDescription = "Обновить")
                            }
                            IconButton(onClick = onOpenApiSettings) {
                                Icon(Icons.Default.Settings, contentDescription = "Настройки API")
                            }
                            IconButton(onClick = onLogout) {
                                Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = "Выйти")
                            }
                        },
                    )
                    HabiHamSegmentTabRow(
                        labels = TrackingTab.entries.map { it.title },
                        selectedIndex = selectedTab,
                        onTabSelected = { selectedTab = it },
                        modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                    )
                }
            },
        ) { padding ->
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding)
                    .imePadding(),
            ) {
                when (tab) {
                    TrackingTab.Habits -> HabitsTab(
                        viewModel = habitsViewModel,
                        modifier = Modifier.fillMaxSize(),
                    )
                    TrackingTab.Todos -> TodosTab(
                        viewModel = todosViewModel,
                        modifier = Modifier.fillMaxSize(),
                    )
                }
            }
        }
    }
}
