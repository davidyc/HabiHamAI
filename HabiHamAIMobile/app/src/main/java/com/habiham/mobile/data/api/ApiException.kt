package com.habiham.mobile.data.api

import com.habiham.mobile.data.model.ApiErrorBody
import retrofit2.HttpException
import java.io.IOException
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException

fun HttpException.userMessage(): String {
    val raw = response()?.errorBody()?.string()
    if (!raw.isNullOrBlank()) {
        runCatching {
            ApiClientFactory.errorBodyAdapter.fromJson(raw)?.message
        }.getOrNull()?.let { return it }
    }
    return when (code()) {
        401 -> "Неверный логин или пароль."
        403 -> "Доступ запрещён."
        in 500..599 -> "Ошибка сервера (${code()})."
        else -> "Ошибка запроса (${code()})."
    }
}

fun Throwable.toUserMessage(): String = when (this) {
    is HttpException -> userMessage()
    is UnknownHostException ->
        "Сервер не найден. Проверьте URL API (на телефоне — IP ПК, не localhost и не 10.0.2.2)."
    is ConnectException ->
        "Не удалось подключиться. Запущен ли API? Для телефона: dotnet run --launch-profile http-mobile"
    is SocketTimeoutException ->
        "Таймаут. Проверьте Wi‑Fi и что API слушает 0.0.0.0:5193, а не только localhost."
    is IOException ->
        "Сеть: ${message ?: "нет связи с сервером"}"
    else -> message ?: "Неизвестная ошибка."
}
