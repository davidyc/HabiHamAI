package com.habiham.mobile.ui.workouts

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.ExpandLess
import androidx.compose.material.icons.filled.ExpandMore
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.TextFieldColors
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.habiham.mobile.domain.CurrentWorkout
import com.habiham.mobile.domain.CurrentWorkoutExercise
import com.habiham.mobile.ui.components.HabiHamBottomBar
import com.habiham.mobile.ui.components.HabiHamContentCard
import com.habiham.mobile.ui.components.SectionTitle

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ActiveWorkoutEditor(
    workout: CurrentWorkout,
    isSaving: Boolean,
    error: String?,
    onDismiss: () -> Unit,
    onDayChange: (String) -> Unit,
    onDateChange: (String) -> Unit,
    onNotesChange: (String) -> Unit,
    onAddExercise: () -> Unit,
    onUpdateExercise: (String, String?, String?) -> Unit,
    onRemoveExercise: (String) -> Unit,
    onAddSet: (String) -> Unit,
    onUpdateSet: (String, Int, String?, String?, String?) -> Unit,
    onRemoveSet: (String, Int) -> Unit,
    onSaveDraft: () -> Unit,
    onFinish: () -> Unit,
) {
    var collapsedExerciseIds by remember { mutableStateOf(setOf<String>()) }

    val fieldColors = OutlinedTextFieldDefaults.colors(
        focusedBorderColor = MaterialTheme.colorScheme.primary,
        unfocusedContainerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f),
        focusedContainerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.35f),
    )

    Dialog(
        onDismissRequest = onDismiss,
        properties = DialogProperties(usePlatformDefaultWidth = false),
    ) {
        Surface(
            modifier = Modifier.fillMaxSize(),
            color = MaterialTheme.colorScheme.background,
        ) {
            Scaffold(
                containerColor = MaterialTheme.colorScheme.background,
                topBar = {
                    TopAppBar(
                        title = {
                            Text(
                                "Активная тренировка",
                                fontWeight = FontWeight.SemiBold,
                            )
                        },
                        navigationIcon = {
                            IconButton(onClick = onDismiss) {
                                Icon(Icons.Default.Close, contentDescription = "Закрыть")
                            }
                        },
                        colors = TopAppBarDefaults.topAppBarColors(
                            containerColor = MaterialTheme.colorScheme.primaryContainer,
                            titleContentColor = MaterialTheme.colorScheme.onPrimaryContainer,
                            navigationIconContentColor = MaterialTheme.colorScheme.onPrimaryContainer,
                        ),
                    )
                },
                bottomBar = {
                    HabiHamBottomBar {
                        Button(
                            onClick = onSaveDraft,
                            enabled = !isSaving,
                            modifier = Modifier.weight(1f),
                            shape = MaterialTheme.shapes.medium,
                            elevation = ButtonDefaults.buttonElevation(defaultElevation = 2.dp),
                        ) {
                            if (isSaving) {
                                CircularProgressIndicator(
                                    strokeWidth = 2.dp,
                                    modifier = Modifier.height(20.dp),
                                )
                            } else {
                                Text("Черновик")
                            }
                        }
                        OutlinedButton(
                            onClick = onFinish,
                            enabled = !isSaving,
                            modifier = Modifier.weight(1f),
                            shape = MaterialTheme.shapes.medium,
                        ) {
                            Text("Завершить")
                        }
                    }
                },
            ) { padding ->
                LazyColumn(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(padding)
                        .padding(horizontal = 16.dp),
                    verticalArrangement = Arrangement.spacedBy(14.dp),
                ) {
                    item {
                        SectionTitle(
                            text = workout.day.ifBlank { "Тренировка" },
                            subtitle = "Заполните подходы и сохраните черновик или завершите",
                        )
                    }

                    item {
                        HabiHamContentCard {
                            OutlinedTextField(
                                value = workout.day,
                                onValueChange = onDayChange,
                                label = { Text("Название") },
                                modifier = Modifier.fillMaxWidth(),
                                singleLine = true,
                                shape = MaterialTheme.shapes.small,
                                colors = fieldColors,
                            )
                            Spacer(Modifier.height(10.dp))
                            OutlinedTextField(
                                value = workout.date,
                                onValueChange = onDateChange,
                                label = { Text("Дата") },
                                modifier = Modifier.fillMaxWidth(),
                                singleLine = true,
                                shape = MaterialTheme.shapes.small,
                                colors = fieldColors,
                            )
                            Spacer(Modifier.height(10.dp))
                            OutlinedTextField(
                                value = workout.notes,
                                onValueChange = onNotesChange,
                                label = { Text("Заметки") },
                                modifier = Modifier.fillMaxWidth(),
                                minLines = 2,
                                shape = MaterialTheme.shapes.small,
                                colors = fieldColors,
                            )
                        }
                    }

                    item {
                        FilledTonalButton(
                            onClick = onAddExercise,
                            modifier = Modifier.fillMaxWidth(),
                            shape = MaterialTheme.shapes.medium,
                        ) {
                            Icon(Icons.Default.Add, contentDescription = null)
                            Spacer(Modifier.width(8.dp))
                            Text("Добавить упражнение")
                        }
                    }

                    if (workout.exercises.isEmpty()) {
                        item {
                            Text(
                                "Нет упражнений. Добавьте первое.",
                                style = MaterialTheme.typography.bodyMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                modifier = Modifier.padding(horizontal = 4.dp),
                            )
                        }
                    }

                    items(workout.exercises, key = { it.localId }) { exercise ->
                        val collapsed = exercise.localId in collapsedExerciseIds
                        ExerciseEditorBlock(
                            exercise = exercise,
                            collapsed = collapsed,
                            onToggleCollapsed = {
                                collapsedExerciseIds = if (collapsed) {
                                    collapsedExerciseIds - exercise.localId
                                } else {
                                    collapsedExerciseIds + exercise.localId
                                }
                            },
                            fieldColors = fieldColors,
                            onUpdateExercise = onUpdateExercise,
                            onRemoveExercise = onRemoveExercise,
                            onAddSet = onAddSet,
                            onUpdateSet = onUpdateSet,
                            onRemoveSet = onRemoveSet,
                        )
                    }

                    error?.let { msg ->
                        item {
                            Text(
                                msg,
                                color = MaterialTheme.colorScheme.error,
                                style = MaterialTheme.typography.bodySmall,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .clip(RoundedCornerShape(8.dp))
                                    .background(MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.5f))
                                    .padding(12.dp),
                            )
                        }
                    }

                    item { Spacer(Modifier.height(8.dp)) }
                }
            }
        }
    }
}

private fun formatSetCountLabel(count: Int): String {
    val n = count.coerceAtLeast(0)
    val mod10 = n % 10
    val mod100 = n % 100
    return when {
        mod10 == 1 && mod100 != 11 -> "$n подход"
        mod10 in 2..4 && mod100 !in 12..14 -> "$n подхода"
        else -> "$n подходов"
    }
}

@Composable
private fun ExerciseEditorBlock(
    exercise: CurrentWorkoutExercise,
    collapsed: Boolean,
    onToggleCollapsed: () -> Unit,
    fieldColors: TextFieldColors,
    onUpdateExercise: (String, String?, String?) -> Unit,
    onRemoveExercise: (String) -> Unit,
    onAddSet: (String) -> Unit,
    onUpdateSet: (String, Int, String?, String?, String?) -> Unit,
    onRemoveSet: (String, Int) -> Unit,
) {
    HabiHamContentCard {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier
                .fillMaxWidth()
                .clip(MaterialTheme.shapes.small)
                .clickable(onClick = onToggleCollapsed)
                .padding(vertical = 4.dp),
        ) {
            IconButton(onClick = onToggleCollapsed) {
                Icon(
                    imageVector = if (collapsed) Icons.Default.ExpandMore else Icons.Default.ExpandLess,
                    contentDescription = if (collapsed) "Развернуть" else "Свернуть",
                    tint = MaterialTheme.colorScheme.primary,
                )
            }
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    exercise.name.ifBlank { "Упражнение" },
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                )
                if (exercise.meta.isNotBlank()) {
                    Text(
                        exercise.meta,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = if (collapsed) 1 else 2,
                    )
                }
                if (collapsed) {
                    Text(
                        formatSetCountLabel(exercise.sets.size),
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.padding(top = 2.dp),
                    )
                }
            }
            IconButton(onClick = { onRemoveExercise(exercise.localId) }) {
                Icon(
                    Icons.Default.Delete,
                    contentDescription = "Удалить",
                    tint = MaterialTheme.colorScheme.error,
                )
            }
        }

        if (collapsed) return@HabiHamContentCard

        Spacer(Modifier.height(8.dp))
        OutlinedTextField(
            value = exercise.name,
            onValueChange = { onUpdateExercise(exercise.localId, it, null) },
            label = { Text("Название") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            shape = MaterialTheme.shapes.small,
            colors = fieldColors,
        )
        Spacer(Modifier.height(8.dp))
        OutlinedTextField(
            value = exercise.meta,
            onValueChange = { onUpdateExercise(exercise.localId, null, it) },
            label = { Text("Комментарий") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            shape = MaterialTheme.shapes.small,
            colors = fieldColors,
        )
        Spacer(Modifier.height(12.dp))
        Text(
            "Подходы",
            style = MaterialTheme.typography.labelLarge,
            color = MaterialTheme.colorScheme.primary,
        )
        Spacer(Modifier.height(8.dp))
        exercise.sets.forEachIndexed { setIndex, set ->
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 4.dp)
                    .clip(MaterialTheme.shapes.small)
                    .background(MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.65f))
                    .padding(8.dp),
                horizontalArrangement = Arrangement.spacedBy(6.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(
                    "${setIndex + 1}",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.width(20.dp),
                )
                OutlinedTextField(
                    value = set.weight,
                    onValueChange = { onUpdateSet(exercise.localId, setIndex, it, null, null) },
                    label = { Text("кг") },
                    modifier = Modifier.weight(1f),
                    singleLine = true,
                    shape = MaterialTheme.shapes.extraSmall,
                    colors = fieldColors,
                )
                OutlinedTextField(
                    value = set.reps,
                    onValueChange = { onUpdateSet(exercise.localId, setIndex, null, it, null) },
                    label = { Text("повт") },
                    modifier = Modifier.weight(1f),
                    singleLine = true,
                    shape = MaterialTheme.shapes.extraSmall,
                    colors = fieldColors,
                )
                OutlinedTextField(
                    value = set.rpe,
                    onValueChange = { onUpdateSet(exercise.localId, setIndex, null, null, it) },
                    label = { Text("RPE") },
                    modifier = Modifier.width(68.dp),
                    singleLine = true,
                    shape = MaterialTheme.shapes.extraSmall,
                    colors = fieldColors,
                )
                if (exercise.sets.size > 1) {
                    IconButton(
                        onClick = { onRemoveSet(exercise.localId, setIndex) },
                        modifier = Modifier.width(40.dp),
                    ) {
                        Icon(
                            Icons.Default.Delete,
                            contentDescription = "Удалить подход",
                            tint = MaterialTheme.colorScheme.error.copy(alpha = 0.8f),
                        )
                    }
                }
            }
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(
            onClick = { onAddSet(exercise.localId) },
            modifier = Modifier.fillMaxWidth(),
            shape = MaterialTheme.shapes.small,
        ) {
            Text("+ Подход")
        }
    }
}
