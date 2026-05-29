package com.habiham.mobile.ui.workouts

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.habiham.mobile.ui.components.HabiHamSegmentTabRow

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
        HabiHamSegmentTabRow(
            labels = StrengthSubTab.entries.map { it.title },
            selectedIndex = subTab,
            onTabSelected = { subTab = it },
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
        )
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
