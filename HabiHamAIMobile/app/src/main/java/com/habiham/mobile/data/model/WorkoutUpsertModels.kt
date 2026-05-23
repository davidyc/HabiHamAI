package com.habiham.mobile.data.model

import com.squareup.moshi.JsonClass

@JsonClass(generateAdapter = true)
data class UpsertWorkoutSessionRequest(
    val sessionCode: String,
    val date: String,
    val day: String,
    val notes: String? = null,
    val isActive: Boolean? = null,
    val exercises: List<UpsertWorkoutExerciseRequest> = emptyList(),
)

@JsonClass(generateAdapter = true)
data class UpsertWorkoutExerciseRequest(
    val name: String,
    val meta: String? = null,
    val sets: List<UpsertWorkoutSetRequest> = emptyList(),
)

@JsonClass(generateAdapter = true)
data class UpsertWorkoutSetRequest(
    val weight: String? = null,
    val reps: String? = null,
    val rpe: String? = null,
)
