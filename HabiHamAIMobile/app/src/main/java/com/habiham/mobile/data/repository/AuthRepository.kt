package com.habiham.mobile.data.repository

import com.habiham.mobile.data.api.ApiClientFactory
import com.habiham.mobile.data.api.UnauthorizedHint
import com.habiham.mobile.data.api.toUserMessage
import com.habiham.mobile.data.model.LoginRequest
import com.habiham.mobile.data.model.RegisterRequest
import com.habiham.mobile.data.prefs.ApiSettingsStore
import com.habiham.mobile.data.prefs.CredentialsStore
import com.habiham.mobile.data.prefs.SessionStore
import retrofit2.HttpException

class AuthRepository(
    private val sessionStore: SessionStore,
    private val apiSettingsStore: ApiSettingsStore,
    private val credentialsStore: CredentialsStore,
) {
    suspend fun login(username: String, password: String): Result<Unit> {
        val apiBaseUrl = apiSettingsStore.getApiBaseUrl()
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
            sessionStore.saveToken(token)
            credentialsStore.save(username, password)
        }.fold(
            onSuccess = { Result.success(Unit) },
            onFailure = { Result.failure(Exception(it.toUserMessage(UnauthorizedHint.Login), it)) },
        )
    }

    suspend fun register(username: String, password: String): Result<String> {
        val apiBaseUrl = apiSettingsStore.getApiBaseUrl()
        return runCatching {
            val api = ApiClientFactory.create(apiBaseUrl)
            val response = api.register(
                RegisterRequest(
                    username = username.trim(),
                    password = password,
                ),
            )
            response.message?.takeIf { it.isNotBlank() }
                ?: "Аккаунт создан."
        }.fold(
            onSuccess = { Result.success(it) },
            onFailure = { err ->
                Result.failure(Exception(registerErrorMessage(err), err))
            },
        )
    }

    suspend fun reloginWithStoredCredentials(): Result<Unit> {
        val credentials = credentialsStore.get()
            ?: return Result.failure(Exception("Сохранённые учётные данные не найдены."))
        val result = login(credentials.username, credentials.password)
        if (result.isFailure) {
            sessionStore.clearToken()
        }
        return result
    }

    suspend fun restoreSessionOnStartup(): Result<Unit> {
        if (!sessionStore.getAccessToken().isNullOrBlank()) {
            return Result.success(Unit)
        }
        return reloginWithStoredCredentials()
    }

    suspend fun logout() {
        sessionStore.clearToken()
        credentialsStore.clear()
    }

    fun peekSavedUsername(): String? = credentialsStore.get()?.username

    private fun registerErrorMessage(err: Throwable): String {
        if (err is HttpException) {
            when (err.code()) {
                409 -> return "Пользователь с таким логином уже существует."
                400 -> return "Проверьте логин и пароль (минимум 6 символов)."
            }
        }
        return err.toUserMessage(UnauthorizedHint.Login)
    }
}
