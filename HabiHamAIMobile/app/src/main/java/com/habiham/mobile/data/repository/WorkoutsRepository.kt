package com.habiham.mobile.data.repository

import com.habiham.mobile.data.api.AuthenticatedApiFactory
import com.habiham.mobile.data.api.toUserMessage
import com.habiham.mobile.data.model.UpsertWorkoutSessionRequest
import com.habiham.mobile.data.model.WorkoutSessionDto
import com.habiham.mobile.data.prefs.StoredSession

data class WorkoutHistoryFilters(
    val from: String? = null,
    val to: String? = null,
    val program: String? = null,
)

class WorkoutsRepository(
    private val apiFactory: AuthenticatedApiFactory,
) {
    suspend fun loadSessions(
        session: StoredSession,
        includeHistory: Boolean = false,
    ): Result<List<WorkoutSessionDto>> = apiCall(session) { api ->
        api.getMyWorkouts(includeHistory = includeHistory)
    }

    suspend fun loadHistory(
        session: StoredSession,
        filters: WorkoutHistoryFilters,
    ): Result<List<WorkoutSessionDto>> = runCatching {
        val api = apiFactory.create(session.apiBaseUrl)
        val program = filters.program?.trim()?.takeIf { it.isNotEmpty() }
        api.getWorkoutHistory(
            from = filters.from?.takeIf { it.isNotBlank() },
            to = filters.to?.takeIf { it.isNotBlank() },
            program = program,
        ).sortedByDescending { it.displayDate() ?: "" }
    }.fold(
        onSuccess = { Result.success(it) },
        onFailure = { Result.failure(Exception(it.toUserMessage(), it)) },
    )

    suspend fun loadProgramOptions(session: StoredSession): Result<List<String>> {
        return runCatching {
            val api = apiFactory.create(session.apiBaseUrl)
            api.getHistoryProgramOptions()
                .mapNotNull { it.program.trim().takeIf { name -> name.isNotEmpty() } }
                .distinct()
                .sorted()
        }.fold(
            onSuccess = { Result.success(it) },
            onFailure = { Result.failure(Exception(it.toUserMessage(), it)) },
        )
    }

    suspend fun upsertWorkout(
        session: StoredSession,
        request: UpsertWorkoutSessionRequest,
    ): Result<WorkoutSessionDto> = apiCall(session) { api ->
        api.upsertWorkout(request)
    }

    private suspend fun <T> apiCall(
        session: StoredSession,
        block: suspend (com.habiham.mobile.data.api.HabiHamApi) -> T,
    ): Result<T> = runCatching {
        val api = apiFactory.create(session.apiBaseUrl)
        block(api)
    }.fold(
        onSuccess = { Result.success(it) },
        onFailure = { Result.failure(Exception(it.toUserMessage(), it)) },
    )
}
