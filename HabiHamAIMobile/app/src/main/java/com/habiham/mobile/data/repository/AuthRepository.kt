package com.habiham.mobile.data.repository

import com.habiham.mobile.data.api.ApiClientFactory
import com.habiham.mobile.data.api.toUserMessage
import com.habiham.mobile.data.model.LoginRequest
import com.habiham.mobile.data.prefs.SessionStore

class AuthRepository(
    private val sessionStore: SessionStore,
) {
    suspend fun login(username: String, password: String, apiBaseUrl: String): Result<Unit> {
        return runCatching {
            val api = ApiClientFactory.create(apiBaseUrl)
            val response = api.login(
                LoginRequest(
                    username = username.trim(),
                    password = password,
                ),
            )
            val token = response.accessToken.trim()
            require(token.isNotEmpty()) { "Сервер не вернул токен." }
            sessionStore.save(token, apiBaseUrl)
        }.fold(
            onSuccess = { Result.success(Unit) },
            onFailure = { Result.failure(Exception(it.toUserMessage(), it)) },
        )
    }

    suspend fun logout() {
        sessionStore.clear()
    }
}
