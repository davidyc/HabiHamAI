package com.habiham.mobile.domain

import com.habiham.mobile.data.model.UpsertWorkoutExerciseRequest
import com.habiham.mobile.data.model.UpsertWorkoutSessionRequest
import com.habiham.mobile.data.model.UpsertWorkoutSetRequest
import com.habiham.mobile.data.model.WorkoutSessionDto
import com.habiham.mobile.util.displayExerciseComment
import com.habiham.mobile.util.todayIso
import java.util.UUID

data class CurrentWorkoutSet(
    val weight: String = "",
    val reps: String = "",
    val rpe: String = "8",
)

data class CurrentWorkoutExercise(
    val localId: String = UUID.randomUUID().toString(),
    val name: String = "",
    val meta: String = "",
    val sets: List<CurrentWorkoutSet> = listOf(CurrentWorkoutSet()),
)

data class CurrentWorkout(
    val sessionCode: String,
    val day: String,
    val date: String,
    val notes: String = "",
    val isActive: Boolean = true,
    val exercises: List<CurrentWorkoutExercise> = emptyList(),
) {
    fun toUpsertRequest(finish: Boolean): UpsertWorkoutSessionRequest {
        return UpsertWorkoutSessionRequest(
            sessionCode = sessionCode,
            date = date.take(10),
            day = day.trim(),
            notes = notes,
            isActive = !finish,
            exercises = exercises
                .map { ex ->
                    UpsertWorkoutExerciseRequest(
                        name = ex.name.trim(),
                        meta = ex.meta.ifBlank { null },
                        sets = ex.sets.map { s ->
                            UpsertWorkoutSetRequest(
                                weight = s.weight.ifBlank { null },
                                reps = s.reps.ifBlank { null },
                                rpe = s.rpe.ifBlank { null },
                            )
                        },
                    )
                }
                .filter { it.name.isNotEmpty() },
        )
    }
}

fun WorkoutSessionDto.toCurrentWorkout(): CurrentWorkout {
    val dateStr = displayDate()?.take(10) ?: todayIso()
    return CurrentWorkout(
        sessionCode = sessionCode.orEmpty(),
        day = day.orEmpty(),
        date = dateStr,
        notes = notes.orEmpty(),
        isActive = isActive != false,
        exercises = exercises.map { ex ->
            val setsSource = ex.sets.takeIf { it.isNotEmpty() }.orEmpty()
            CurrentWorkoutExercise(
                localId = ex.id,
                name = ex.name.orEmpty(),
                meta = displayExerciseComment(ex.meta),
                sets = if (setsSource.isEmpty()) {
                    listOf(CurrentWorkoutSet())
                } else {
                    setsSource.map { s ->
                        CurrentWorkoutSet(
                            weight = s.weight.orEmpty(),
                            reps = s.reps.orEmpty(),
                            rpe = s.rpe?.ifBlank { "8" } ?: "8",
                        )
                    }
                },
            )
        },
    )
}

fun currentWorkoutFromProgram(program: WorkoutSessionDto): CurrentWorkout {
    return CurrentWorkout(
        sessionCode = "workout::${System.currentTimeMillis()}",
        day = program.day?.ifBlank { "Тренировка" } ?: "Тренировка",
        date = todayIso(),
        notes = "",
        isActive = true,
        exercises = program.exercises.map { ex ->
            CurrentWorkoutExercise(
                name = ex.name.orEmpty(),
                meta = displayExerciseComment(ex.meta),
                sets = listOf(CurrentWorkoutSet()),
            )
        },
    )
}

fun currentWorkoutFromScratch(): CurrentWorkout {
    return CurrentWorkout(
        sessionCode = "workout::${System.currentTimeMillis()}",
        day = "Новая тренировка",
        date = todayIso(),
        isActive = true,
    )
}
