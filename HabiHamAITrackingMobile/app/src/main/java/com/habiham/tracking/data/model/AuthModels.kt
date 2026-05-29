package com.habiham.tracking.data.model

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
