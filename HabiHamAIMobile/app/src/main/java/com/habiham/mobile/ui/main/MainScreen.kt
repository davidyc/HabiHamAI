package com.habiham.mobile.ui.main

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.DirectionsBike
import androidx.compose.material.icons.filled.FitnessCenter
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import com.habiham.mobile.ui.bike.BikeTab
import com.habiham.mobile.ui.bike.BikeViewModel
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
    onLogout: () -> Unit,
) {
    var selectedTab by rememberSaveable { mutableIntStateOf(0) }
    val tab = HomeTab.entries[selectedTab]

    Scaffold(
        topBar = {
            Column {
                TopAppBar(
                    title = { Text("HabiHam") },
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
                        IconButton(onClick = onLogout) {
                            Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = "Выйти")
                        }
                    },
                )
                TabRow(selectedTabIndex = selectedTab) {
                    HomeTab.entries.forEachIndexed { index, item ->
                        Tab(
                            selected = selectedTab == index,
                            onClick = { selectedTab = index },
                            text = { Text(item.title) },
                            icon = { Icon(item.icon, contentDescription = item.title) },
                        )
                    }
                }
            }
        },
    ) { padding ->
        when (tab) {
            HomeTab.Strength -> WorkoutsRootTab(
                activeViewModel = activeWorkoutViewModel,
                historyViewModel = workoutsViewModel,
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding),
            )
            HomeTab.Bike -> BikeTab(
                viewModel = bikeViewModel,
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding),
            )
        }
    }
}
