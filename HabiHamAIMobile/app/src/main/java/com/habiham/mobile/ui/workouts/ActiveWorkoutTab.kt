package com.habiham.mobile.ui.workouts

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
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
import com.habiham.mobile.ui.components.HabiHamAccentCard
import com.habiham.mobile.ui.components.SectionTitle
import com.habiham.mobile.ui.components.scrollWithIme
import com.habiham.mobile.util.formatWorkoutDateLabel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ActiveWorkoutTab(
    viewModel: ActiveWorkoutViewModel,
    modifier: Modifier = Modifier,
) {
    val state by viewModel.uiState.collectAsState()

    if (state.isEditorOpen && state.currentWorkout != null) {
        ActiveWorkoutEditor(
            workout = state.currentWorkout!!,
            isSaving = state.isSaving,
            error = state.error,
            onDismiss = viewModel::closeEditor,
            onDayChange = { viewModel.updateWorkoutField(day = it) },
            onDateChange = { viewModel.updateWorkoutField(date = it) },
            onNotesChange = { viewModel.updateWorkoutField(notes = it) },
            onAddExercise = viewModel::addExercise,
            onUpdateExercise = viewModel::updateExercise,
            onRemoveExercise = viewModel::removeExercise,
            onAddSet = viewModel::addSet,
            onUpdateSet = viewModel::updateSet,
            onRemoveSet = viewModel::removeSet,
            onSaveDraft = { viewModel.persist(finish = false) },
            onFinish = { viewModel.persist(finish = true) },
        )
        return
    }

    Column(
        modifier = modifier
            .fillMaxSize()
            .scrollWithIme()
            .verticalScroll(rememberScrollState())
            .padding(16.dp),
    ) {
        SectionTitle(
            text = "Моя тренировка",
            subtitle = "Начните с программы или создайте тренировку с нуля",
        )

        if (state.isLoading) {
            Column(
                Modifier.fillMaxWidth(),
                horizontalAlignment = Alignment.CenterHorizontally,
            ) {
                CircularProgressIndicator()
            }
            return@Column
        }

        state.error?.let { msg ->
            Text(msg, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
            Spacer(Modifier.height(8.dp))
        }

        ProgramPicker(
            programs = state.programs,
            selectedCode = state.selectedProgramCode,
            onSelect = viewModel::selectProgram,
        )
        Spacer(Modifier.height(8.dp))

        Button(
            onClick = viewModel::startFromSelectedProgram,
            enabled = state.selectedProgramCode != null,
            modifier = Modifier.fillMaxWidth(),
            shape = MaterialTheme.shapes.medium,
            elevation = ButtonDefaults.buttonElevation(defaultElevation = 2.dp),
        ) {
            Text("Начать тренировку")
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(
            onClick = viewModel::startFromScratch,
            modifier = Modifier.fillMaxWidth(),
            shape = MaterialTheme.shapes.medium,
        ) {
            Text("Создать с нуля")
        }

        val bannerWorkout = state.currentWorkout ?: state.activeSession
        if (bannerWorkout != null) {
            Spacer(Modifier.height(16.dp))
            HabiHamAccentCard {
                Text(
                    "Активная",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onPrimaryContainer,
                )
                val title = state.currentWorkout?.day
                    ?: state.activeSession?.day
                    ?: "Тренировка"
                Text(
                    title,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onPrimaryContainer,
                )
                val dateIso = state.currentWorkout?.date
                    ?: state.activeSession?.displayDate()
                Text(
                    formatWorkoutDateLabel(dateIso),
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onPrimaryContainer,
                )
                Spacer(Modifier.height(10.dp))
                Button(
                    onClick = { viewModel.openEditor() },
                    modifier = Modifier.fillMaxWidth(),
                    shape = MaterialTheme.shapes.medium,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.primary,
                        contentColor = MaterialTheme.colorScheme.onPrimary,
                    ),
                ) {
                    Text("Открыть тренировку")
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ProgramPicker(
    programs: List<com.habiham.mobile.data.model.WorkoutSessionDto>,
    selectedCode: String?,
    onSelect: (String?) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    val label = programs.find { it.sessionCode == selectedCode }?.day
        ?: "Выберите программу"

    ExposedDropdownMenuBox(
        expanded = expanded,
        onExpandedChange = { expanded = !expanded },
        modifier = Modifier.fillMaxWidth(),
    ) {
        OutlinedTextField(
            value = label,
            onValueChange = {},
            readOnly = true,
            label = { Text("Программа") },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
            modifier = Modifier
                .menuAnchor()
                .fillMaxWidth(),
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            DropdownMenuItem(
                text = { Text("Нет выбранной программы") },
                onClick = {
                    onSelect(null)
                    expanded = false
                },
            )
            programs.forEach { program ->
                DropdownMenuItem(
                    text = { Text(program.day ?: program.sessionCode.orEmpty()) },
                    onClick = {
                        onSelect(program.sessionCode)
                        expanded = false
                    },
                )
            }
        }
    }
}
