package com.habiham.tracking.data.repository

import com.habiham.tracking.data.api.AuthenticatedApiFactory
import com.habiham.tracking.data.model.CreateHabitRequest
import com.habiham.tracking.data.model.UpdateHabitRequest
import com.habiham.tracking.data.model.CreateTodoRequest
import com.habiham.tracking.data.model.HabitCheckinDto
import com.habiham.tracking.data.model.HabitOverviewDto
import com.habiham.tracking.data.model.TodoItemDto
import com.habiham.tracking.data.model.UpsertHabitCheckinRequest
import com.habiham.tracking.data.model.UpsertTodoDoneRequest
import com.habiham.tracking.data.model.UserCategoryDto
import com.habiham.tracking.data.prefs.StoredSession
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope

class TrackingRepository(
    private val apiFactory: AuthenticatedApiFactory,
) {
    suspend fun loadCategories(session: StoredSession): Result<List<UserCategoryDto>> =
        apiCall(apiFactory, session) { it.getCategories() }

    suspend fun loadHabitsOverview(session: StoredSession): Result<List<HabitOverviewDto>> =
        apiCall(apiFactory, session) { it.getHabitsOverview() }

    suspend fun loadCheckinsForHabits(
        session: StoredSession,
        habits: List<HabitOverviewDto>,
        from: String?,
        to: String?,
    ): Result<Map<String, Map<String, String>>> = runCatching {
        coroutineScope {
            habits.map { habit ->
                async {
                    val result = apiCall(apiFactory, session) { api ->
                        api.getHabitCheckins(
                            habitId = habit.id,
                            from = from?.takeIf { it.isNotBlank() },
                            to = to?.takeIf { it.isNotBlank() },
                        )
                    }
                    habit.id to (result.getOrElse { emptyList() })
                }
            }.awaitAll()
        }.associate { (habitId, entries) ->
            habitId to entries.associate { dto ->
                dto.date.take(10) to dto.status.lowercase()
            }
        }
    }.fold(
        onSuccess = { Result.success(it) },
        onFailure = { Result.failure(Exception(it.message ?: "Ошибка загрузки отметок", it)) },
    )

    suspend fun createHabit(
        session: StoredSession,
        name: String,
        categoryId: String?,
        daysToMaster: Int = 21,
    ): Result<Unit> = apiCall(apiFactory, session) {
        it.createHabit(
            CreateHabitRequest(
                name = name.trim(),
                categoryId = categoryId,
                daysToMaster = daysToMaster,
            ),
        )
    }

    suspend fun updateHabit(
        session: StoredSession,
        habitId: String,
        name: String,
        categoryId: String?,
        daysToMaster: Int,
    ): Result<Unit> = apiCall(apiFactory, session) {
        it.updateHabit(
            habitId = habitId,
            body = UpdateHabitRequest(
                name = name.trim(),
                categoryId = categoryId,
                daysToMaster = daysToMaster,
            ),
        )
    }

    suspend fun deleteHabit(session: StoredSession, habitId: String): Result<Unit> =
        deleteCall(apiFactory, session) { it.deleteHabit(habitId) }

    suspend fun upsertHabitCheckin(
        session: StoredSession,
        habitId: String,
        date: String,
        status: String,
    ): Result<HabitCheckinDto> = apiCall(apiFactory, session) {
        it.upsertHabitCheckin(
            habitId = habitId,
            body = UpsertHabitCheckinRequest(date = date, status = status),
        )
    }

    suspend fun deleteHabitCheckin(
        session: StoredSession,
        habitId: String,
        date: String,
    ): Result<Unit> = deleteCall(apiFactory, session) {
        it.deleteHabitCheckin(habitId = habitId, date = date)
    }

    suspend fun loadTodos(
        session: StoredSession,
        from: String?,
        to: String?,
    ): Result<List<TodoItemDto>> = apiCall(apiFactory, session) {
        it.getTodos(
            from = from?.takeIf { it.isNotBlank() },
            to = to?.takeIf { it.isNotBlank() },
        )
    }

    suspend fun createTodo(
        session: StoredSession,
        title: String,
        dueDate: String?,
        categoryId: String?,
    ): Result<TodoItemDto> = apiCall(apiFactory, session) {
        it.createTodo(
            CreateTodoRequest(
                title = title.trim(),
                dueDate = dueDate?.takeIf { it.isNotBlank() },
                categoryId = categoryId,
            ),
        )
    }

    suspend fun deleteTodo(session: StoredSession, todoId: String): Result<Unit> =
        deleteCall(apiFactory, session) { it.deleteTodo(todoId) }

    suspend fun setTodoDone(
        session: StoredSession,
        todoId: String,
        isDone: Boolean,
        date: String?,
    ): Result<TodoItemDto> = apiCall(apiFactory, session) {
        it.upsertTodoDone(
            todoId = todoId,
            body = UpsertTodoDoneRequest(isDone = isDone, date = date),
        )
    }
}
