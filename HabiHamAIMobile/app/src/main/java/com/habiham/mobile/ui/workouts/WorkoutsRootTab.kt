package com.habiham.mobile.ui.workouts

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier

private enum class StrengthSubTab(val title: String) {
    Current("Текущая"),
    History("История"),
}

@Composable
fun WorkoutsRootTab(
    activeViewModel: ActiveWorkoutViewModel,
    historyViewModel: WorkoutsViewModel,
    modifier: Modifier = Modifier,
) {
    var subTab by rememberSaveable { mutableIntStateOf(0) }
    val selected = StrengthSubTab.entries[subTab]

    Column(modifier = modifier.fillMaxSize()) {
        TabRow(selectedTabIndex = subTab) {
            StrengthSubTab.entries.forEachIndexed { index, tab ->
                Tab(
                    selected = subTab == index,
                    onClick = { subTab = index },
                    text = { Text(tab.title) },
                )
            }
        }
        when (selected) {
            StrengthSubTab.Current -> ActiveWorkoutTab(
                viewModel = activeViewModel,
                modifier = Modifier.fillMaxSize(),
            )
            StrengthSubTab.History -> WorkoutsTab(
                viewModel = historyViewModel,
                modifier = Modifier.fillMaxSize(),
            )
        }
    }
}
