package com.habiham.tracking.data.model

import com.squareup.moshi.JsonClass

@JsonClass(generateAdapter = true)
data class UserCategoryDto(
    val id: String,
    val name: String,
    val description: String? = null,
    val isActive: Boolean = true,
    val sortOrder: Int = 0,
)

@JsonClass(generateAdapter = true)
data class HabitOverviewDto(
    val id: String,
    val name: String,
    val categoryId: String? = null,
    val categoryName: String? = null,
    val isActive: Boolean = true,
    val createdAtUtc: String? = null,
    val currentStreakDays: Int = 0,
    val isDoneToday: Boolean = false,
    val todayStatus: String? = null,
    val lastDoneDate: String? = null,
    val isMastered: Boolean = false,
    val daysToMaster: Int = 21,
)

@JsonClass(generateAdapter = true)
data class CreateHabitRequest(
    val name: String,
    val categoryId: String? = null,
    val daysToMaster: Int = 21,
)

@JsonClass(generateAdapter = true)
data class UpdateHabitRequest(
    val name: String,
    val categoryId: String? = null,
    val daysToMaster: Int = 21,
)

@JsonClass(generateAdapter = true)
data class HabitCheckinDto(
    val date: String,
    val status: String,
    val id: String? = null,
)

@JsonClass(generateAdapter = true)
data class UpsertHabitCheckinRequest(
    val date: String,
    val status: String,
)

@JsonClass(generateAdapter = true)
data class TodoItemDto(
    val id: String,
    val title: String,
    val categoryId: String? = null,
    val categoryName: String? = null,
    val dueDate: String? = null,
    val doneDate: String? = null,
    val isDone: Boolean = false,
    val createdAtUtc: String? = null,
)

@JsonClass(generateAdapter = true)
data class CreateTodoRequest(
    val title: String,
    val dueDate: String? = null,
    val categoryId: String? = null,
)

@JsonClass(generateAdapter = true)
data class UpsertTodoDoneRequest(
    val isDone: Boolean,
    val date: String? = null,
)
