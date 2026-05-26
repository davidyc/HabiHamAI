package com.habiham.mobile.data.model

import com.squareup.moshi.Json
import com.squareup.moshi.JsonClass

@JsonClass(generateAdapter = true)
data class LoginRequest(
    val username: String,
    val password: String,
)

@JsonClass(generateAdapter = true)
data class LoginResponse(
    val accessToken: String,
    val tokenType: String? = null,
)

@JsonClass(generateAdapter = true)
data class RegisterRequest(
    val username: String,
    val password: String,
)

@JsonClass(generateAdapter = true)
data class RegisterResponse(
    val message: String? = null,
    val role: String? = null,
)

@JsonClass(generateAdapter = true)
data class ApiErrorBody(
    val message: String? = null,
)

@JsonClass(generateAdapter = true)
data class HistoryProgramOption(
    val program: String,
)

@JsonClass(generateAdapter = true)
data class WorkoutSessionDto(
    val id: String,
    val sessionCode: String? = null,
    val date: String? = null,
    @Json(name = "_date") val legacyDate: String? = null,
    val day: String? = null,
    val notes: String? = null,
    val createdAtUtc: String? = null,
    val isActive: Boolean? = null,
    val exercises: List<WorkoutExerciseDto> = emptyList(),
) {
    fun displayDate(): String? = date?.takeIf { it.isNotBlank() } ?: legacyDate?.takeIf { it.isNotBlank() }
}

@JsonClass(generateAdapter = true)
data class WorkoutExerciseDto(
    val id: String,
    val sessionId: String? = null,
    val name: String? = null,
    val meta: String? = null,
    val order: Int? = null,
    val sets: List<WorkoutSetDto> = emptyList(),
)

@JsonClass(generateAdapter = true)
data class WorkoutSetDto(
    val id: String? = null,
    val weight: String? = null,
    val reps: String? = null,
    val rpe: String? = null,
    val order: Int? = null,
)
