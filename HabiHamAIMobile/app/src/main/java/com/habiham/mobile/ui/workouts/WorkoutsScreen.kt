package com.habiham.mobile.ui.workouts

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.slideInVertically
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button

import com.habiham.mobile.ui.components.habihamTextFieldColors

import com.habiham.mobile.ui.components.AnimatedCollapsibleSection
import com.habiham.mobile.ui.components.CollapsibleExerciseTitle
import com.habiham.mobile.ui.components.HabiHamListCard
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.habiham.mobile.data.model.WorkoutExerciseDto
import com.habiham.mobile.data.model.WorkoutSessionDto
import com.habiham.mobile.ui.components.scrollWithIme
import com.habiham.mobile.util.formatWorkoutDateLabel

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun WorkoutsTab(
    viewModel: WorkoutsViewModel,
    modifier: Modifier = Modifier,
) {
    val state by viewModel.uiState.collectAsState()
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)

    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .scrollWithIme(),
        contentPadding = PaddingValues(bottom = 16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        item {
            FiltersSection(
                state = state,
                onDateFromChange = viewModel::onDateFromChange,
                onDateToChange = viewModel::onDateToChange,
                onProgramSelected = viewModel::onProgramSelected,
                onApplyPreset = viewModel::applyDatePreset,
                onApplyFilters = viewModel::loadHistory,
            )
        }

        if (!state.error.isNullOrBlank()) {
            item {
                Text(
                    text = state.error!!,
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(horizontal = 16.dp),
                    style = MaterialTheme.typography.bodySmall,
                )
            }
        }

        when {
            state.isLoading && state.sessions.isEmpty() -> {
                item {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(160.dp),
                        contentAlignment = Alignment.Center,
                    ) {
                        CircularProgressIndicator()
                    }
                }
            }
            state.sessions.isEmpty() -> {
                item {
                    Text(
                        "Нет тренировок по выбранному фильтру.",
                        modifier = Modifier.padding(horizontal = 16.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
            else -> {
                items(state.sessions, key = { it.id }) { session ->
                    WorkoutSessionCard(
                        session = session,
                        onClick = { viewModel.openSession(session) },
                        modifier = Modifier.padding(horizontal = 16.dp),
                    )
                }
            }
        }
    }

    state.selectedSession?.let { session ->
        ModalBottomSheet(
            onDismissRequest = viewModel::closeSessionDetail,
            sheetState = sheetState,
        ) {
            WorkoutDetailContent(session = session)
        }
    }
}

@OptIn(ExperimentalLayoutApi::class, ExperimentalMaterial3Api::class)
@Composable
private fun FiltersSection(
    state: WorkoutsUiState,
    onDateFromChange: (String) -> Unit,
    onDateToChange: (String) -> Unit,
    onProgramSelected: (String?) -> Unit,
    onApplyPreset: (Long) -> Unit,
    onApplyFilters: () -> Unit,
) {
    var programExpanded by remember { mutableStateOf(false) }
    val programLabel = state.selectedProgram ?: "Все программы"

    Column(modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)) {
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            OutlinedTextField(
                value = state.dateFrom,
                onValueChange = onDateFromChange,
                label = { Text("С") },
                modifier = Modifier.weight(1f),
                singleLine = true,
                placeholder = { Text("yyyy-MM-dd") },
                colors = habihamTextFieldColors(),
                shape = MaterialTheme.shapes.small,
            )
            OutlinedTextField(
                value = state.dateTo,
                onValueChange = onDateToChange,
                label = { Text("По") },
                modifier = Modifier.weight(1f),
                singleLine = true,
                placeholder = { Text("yyyy-MM-dd") },
                colors = habihamTextFieldColors(),
                shape = MaterialTheme.shapes.small,
            )
        }

        Spacer(Modifier.height(8.dp))

        ExposedDropdownMenuBox(
            expanded = programExpanded,
            onExpandedChange = { programExpanded = !programExpanded },
            modifier = Modifier.fillMaxWidth(),
        ) {
            OutlinedTextField(
                value = programLabel,
                onValueChange = {},
                readOnly = true,
                label = { Text("Программа / день") },
                trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = programExpanded) },
                modifier = Modifier
                    .menuAnchor()
                    .fillMaxWidth(),
                colors = habihamTextFieldColors(),
                shape = MaterialTheme.shapes.small,
            )
            ExposedDropdownMenu(
                expanded = programExpanded,
                onDismissRequest = { programExpanded = false },
            ) {
                DropdownMenuItem(
                    text = { Text("Все программы") },
                    onClick = {
                        onProgramSelected(null)
                        programExpanded = false
                    },
                )
                state.programOptions.forEach { program ->
                    DropdownMenuItem(
                        text = { Text(program) },
                        onClick = {
                            onProgramSelected(program)
                            programExpanded = false
                        },
                    )
                }
            }
        }

        Spacer(Modifier.height(8.dp))

        FlowRow(
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            listOf(
                "День" to 1L,
                "Неделя" to 7L,
                "10 дн." to 10L,
                "15 дн." to 15L,
                "Месяц" to 30L,
            ).forEach { (label, days) ->
                FilterChip(
                    selected = false,
                    onClick = { onApplyPreset(days) },
                    label = { Text(label) },
                )
            }
        }

        Spacer(Modifier.height(8.dp))

        Button(
            onClick = onApplyFilters,
            enabled = !state.isLoading,
            modifier = Modifier.fillMaxWidth(),
        ) {
            if (state.isLoading) {
                CircularProgressIndicator(strokeWidth = 2.dp, modifier = Modifier.height(18.dp))
            } else {
                Text("Применить фильтры")
            }
        }
    }
}

@Composable
private fun WorkoutSessionCard(
    session: WorkoutSessionDto,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
) {
    HabiHamListCard(
        modifier = modifier,
        onClick = onClick,
    ) {
            Text(
                text = session.day?.ifBlank { "Тренировка" } ?: "Тренировка",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.SemiBold,
            )
            Spacer(Modifier.height(4.dp))
            Text(
                "Дата: ${formatWorkoutDateLabel(session.displayDate())}",
                style = MaterialTheme.typography.bodyMedium,
            )
            Text(
                "Упражнений: ${session.exercises.size}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            if (!session.notes.isNullOrBlank()) {
                Text(
                    session.notes!!,
                    style = MaterialTheme.typography.bodySmall,
                    modifier = Modifier.padding(top = 4.dp),
                )
            }
    }
}

@Composable
private fun WorkoutDetailContent(session: WorkoutSessionDto) {
    var collapsedExerciseIds by remember(session.id) {
        mutableStateOf(session.exercises.map { it.id }.toSet())
    }
    var exercisesRevealed by remember(session.id) { mutableStateOf(false) }
    LaunchedEffect(session.id) {
        exercisesRevealed = true
    }

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 20.dp, vertical = 8.dp)
            .navigationBarsPadding()
            .padding(bottom = 32.dp),
    ) {
        Text(session.day ?: "Тренировка", style = MaterialTheme.typography.headlineSmall)
        Text(
            formatWorkoutDateLabel(session.displayDate()),
            style = MaterialTheme.typography.bodyLarge,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        if (!session.notes.isNullOrBlank()) {
            Spacer(Modifier.height(8.dp))
            Text(session.notes!!, style = MaterialTheme.typography.bodyMedium)
        }
        Spacer(Modifier.height(16.dp))
        session.exercises.forEachIndexed { index, exercise ->
            val enterDelay = 40 * index
            AnimatedVisibility(
                visible = exercisesRevealed,
                enter = fadeIn(tween(300, delayMillis = enterDelay)) +
                    slideInVertically(
                        animationSpec = tween(300, delayMillis = enterDelay),
                        initialOffsetY = { fullHeight -> fullHeight / 4 },
                    ),
            ) {
                Column {
                    if (index > 0) Spacer(Modifier.height(10.dp))
                    HistoryExerciseBlock(
                        exercise = exercise,
                        expanded = exercise.id !in collapsedExerciseIds,
                        onToggle = {
                            collapsedExerciseIds = if (exercise.id in collapsedExerciseIds) {
                                collapsedExerciseIds - exercise.id
                            } else {
                                collapsedExerciseIds + exercise.id
                            }
                        },
                    )
                }
            }
        }
    }
}

@Composable
private fun HistoryExerciseBlock(
    exercise: WorkoutExerciseDto,
    expanded: Boolean,
    onToggle: () -> Unit,
) {
    HabiHamListCard(onClick = onToggle) {
        AnimatedCollapsibleSection(
            expanded = expanded,
            onToggle = onToggle,
            header = {
                CollapsibleExerciseTitle(
                    name = exercise.name.orEmpty(),
                    meta = exercise.meta,
                    setCount = exercise.sets.size,
                    expanded = expanded,
                )
            },
        ) {
            exercise.sets.forEachIndexed { setIndex, set ->
                val parts = buildList {
                    set.weight?.takeIf { it.isNotBlank() }?.let { add("${it} кг") }
                    set.reps?.takeIf { it.isNotBlank() }?.let { add("$it повт.") }
                    set.rpe?.takeIf { it.isNotBlank() }?.let { add("RPE $it") }
                }
                Text(
                    text = "${setIndex + 1}. ${parts.joinToString(" · ").ifBlank { "—" }}",
                    style = MaterialTheme.typography.bodyMedium,
                    modifier = Modifier.padding(start = 4.dp, top = if (setIndex > 0) 4.dp else 0.dp),
                )
            }
        }
    }
}
