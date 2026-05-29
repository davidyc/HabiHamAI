package com.habiham.mobile.ui.main

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.DirectionsBike
import androidx.compose.material.icons.filled.FitnessCenter
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.habiham.mobile.ui.bike.BikeTab
import com.habiham.mobile.ui.bike.BikeViewModel
import com.habiham.mobile.ui.components.HabiHamBrandTitle
import com.habiham.mobile.ui.components.HabiHamKeyboardInsets
import com.habiham.mobile.ui.components.HabiHamScreen
import com.habiham.mobile.ui.components.HabiHamSegmentTabRow
import com.habiham.mobile.ui.workouts.ActiveWorkoutViewModel
import com.habiham.mobile.ui.workouts.WorkoutsRootTab
import com.habiham.mobile.ui.workouts.WorkoutsViewModel

private enum class HomeTab(val title: String, val icon: ImageVector) {
    Strength("Сила", Icons.Default.FitnessCenter),
    Bike("Вело", Icons.Default.DirectionsBike),
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainScreen(
    workoutsViewModel: WorkoutsViewModel,
    activeWorkoutViewModel: ActiveWorkoutViewModel,
    bikeViewModel: BikeViewModel,
    onOpenApiSettings: () -> Unit,
    onLogout: () -> Unit,
) {
    var selectedTab by rememberSaveable { mutableIntStateOf(0) }
    val tab = HomeTab.entries[selectedTab]

    HabiHamScreen {
        Scaffold(
            modifier = Modifier.fillMaxSize(),
            containerColor = Color.Transparent,
            contentWindowInsets = HabiHamKeyboardInsets.scaffoldContent,
            topBar = {
                Column {
                    TopAppBar(
                        title = { HabiHamBrandTitle("HabiHam") },
                        colors = TopAppBarDefaults.topAppBarColors(
                            containerColor = MaterialTheme.colorScheme.surfaceVariant,
                            titleContentColor = MaterialTheme.colorScheme.onSurface,
                            navigationIconContentColor = MaterialTheme.colorScheme.onSurface,
                            actionIconContentColor = MaterialTheme.colorScheme.onSurface,
                        ),
                        actions = {
                            IconButton(
                                onClick = {
                                    when (tab) {
                                        HomeTab.Strength -> {
                                            activeWorkoutViewModel.refresh()
                                            workoutsViewModel.loadHistory()
                                        }
                                        HomeTab.Bike -> bikeViewModel.loadActivities()
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
                        labels = HomeTab.entries.map { it.title },
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
                    HomeTab.Strength -> WorkoutsRootTab(
                        activeViewModel = activeWorkoutViewModel,
                        historyViewModel = workoutsViewModel,
                        modifier = Modifier.fillMaxSize(),
                    )
                    HomeTab.Bike -> BikeTab(
                        viewModel = bikeViewModel,
                        modifier = Modifier.fillMaxSize(),
                    )
                }
            }
        }
    }
}
