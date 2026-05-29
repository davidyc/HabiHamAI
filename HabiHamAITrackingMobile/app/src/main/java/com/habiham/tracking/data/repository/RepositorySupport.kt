package com.habiham.tracking.data.repository

import com.habiham.tracking.data.api.AuthenticatedApiFactory
import com.habiham.tracking.data.api.TrackingApi
import com.habiham.tracking.data.api.toUserMessage
import com.habiham.tracking.data.prefs.StoredSession
import retrofit2.HttpException
import retrofit2.Response

internal suspend fun <T> apiCall(
    apiFactory: AuthenticatedApiFactory,
    session: StoredSession,
    block: suspend (TrackingApi) -> T,
): Result<T> = runCatching {
    val api = apiFactory.create(session.apiBaseUrl)
    block(api)
}.fold(
    onSuccess = { Result.success(it) },
    onFailure = { Result.failure(Exception(it.toUserMessage(), it)) },
)

internal suspend fun deleteCall(
    apiFactory: AuthenticatedApiFactory,
    session: StoredSession,
    block: suspend (TrackingApi) -> Response<Unit>,
): Result<Unit> = runCatching {
    val api = apiFactory.create(session.apiBaseUrl)
    val response = block(api)
    if (!response.isSuccessful && response.code() != 204) {
        throw HttpException(response)
    }
}.fold(
    onSuccess = { Result.success(Unit) },
    onFailure = { Result.failure(Exception(it.toUserMessage(), it)) },
)
