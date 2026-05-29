package com.habiham.tracking.data.api

import com.habiham.tracking.data.model.ApiErrorBody
import retrofit2.HttpException
import java.io.IOException
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

enum class UnauthorizedHint {
    Login,
    Api,
}

fun HttpException.userMessage(hint: UnauthorizedHint = UnauthorizedHint.Api): String {
    val raw = response()?.errorBody()?.string()
    if (!raw.isNullOrBlank()) {
        runCatching {
            ApiClientFactory.errorBodyAdapter.fromJson(raw)?.message
        }.getOrNull()?.let { return it }
    }
    return when (code()) {
        401 -> when (hint) {
            UnauthorizedHint.Login -> "Неверный логин или пароль."
            UnauthorizedHint.Api -> "Сессия истекла. Войдите снова."
        }
        403 -> "Доступ запрещён."
        in 500..599 -> "Ошибка сервера (${code()})."
        else -> "Ошибка запроса (${code()})."
    }
}

fun Throwable.toUserMessage(hint: UnauthorizedHint = UnauthorizedHint.Api): String = when (this) {
    is HttpException -> userMessage(hint)
    is UnknownHostException ->
        "Сервер не найден. Проверьте URL API (на телефоне — IP ПК, не localhost)."
    is ConnectException ->
        "Не удалось подключиться. Запущен ли API?"
    is SocketTimeoutException -> "Таймаут. Проверьте сеть и адрес API."
    is IOException -> "Сеть: ${message ?: "нет связи с сервером"}"
    else -> message ?: "Неизвестная ошибка."
}
