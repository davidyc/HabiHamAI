package com.habiham.mobile.data.api

import com.habiham.mobile.data.auth.SessionRefreshCoordinator
import com.habiham.mobile.data.model.ApiErrorBody
import com.habiham.mobile.data.prefs.SessionStore
import com.squareup.moshi.Moshi
import com.squareup.moshi.kotlin.reflect.KotlinJsonAdapterFactory
import kotlinx.coroutines.runBlocking
import okhttp3.Authenticator
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.Response
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.moshi.MoshiConverterFactory
import java.util.concurrent.TimeUnit

object ApiClientFactory {
    private val moshi: Moshi = Moshi.Builder()
        .add(KotlinJsonAdapterFactory())
        .build()

    val errorBodyAdapter = moshi.adapter(ApiErrorBody::class.java)

    private val anonymousAuthPaths = setOf(
        "/auth/login",
        "/auth/register",
    )

    fun create(baseUrl: String): HabiHamApi =
        buildRetrofit(baseUrl, OkHttpClient.Builder().build())

    fun createAuthenticated(
        baseUrl: String,
        sessionStore: SessionStore,
        refreshCoordinator: SessionRefreshCoordinator,
    ): HabiHamApi {
        val authInterceptor = Interceptor { chain ->
            val request = chain.request()
            val builder = request.newBuilder()
            if (!isAnonymousAuthPath(request.url.encodedPath)) {
                val token = runBlocking { sessionStore.getAccessToken() }
                if (!token.isNullOrBlank()) {
                    builder.header("Authorization", "Bearer $token")
                }
            }
            chain.proceed(builder.build())
        }

        val authenticator = Authenticator { _, response ->
            if (response.code != 401) return@Authenticator null
            if (isAnonymousAuthPath(response.request.url.encodedPath)) return@Authenticator null
            if (response.priorResponseCount() >= 2) return@Authenticator null

            val refreshed = runBlocking { refreshCoordinator.refreshSession() }
            if (!refreshed) return@Authenticator null

            val newToken = runBlocking { sessionStore.getAccessToken() }
            if (newToken.isNullOrBlank()) return@Authenticator null

            response.request.newBuilder()
                .header("Authorization", "Bearer $newToken")
                .build()
        }

        val client = OkHttpClient.Builder()
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(60, TimeUnit.SECONDS)
            .addInterceptor(authInterceptor)
            .authenticator(authenticator)
            .addInterceptor(loggingInterceptor())
            .build()

        return buildRetrofit(baseUrl, client)
    }

    private fun buildRetrofit(baseUrl: String, httpClient: OkHttpClient): HabiHamApi {
        val normalized = baseUrl.trim().trimEnd('/') + "/"
        val client = httpClient.newBuilder()
            .apply {
                if (httpClient.interceptors.none { it is HttpLoggingInterceptor }) {
                    addInterceptor(loggingInterceptor())
                }
            }
            .build()

        return Retrofit.Builder()
            .baseUrl(normalized)
            .client(client)
            .addConverterFactory(MoshiConverterFactory.create(moshi))
            .build()
            .create(HabiHamApi::class.java)
    }

    private fun loggingInterceptor(): HttpLoggingInterceptor =
        HttpLoggingInterceptor().apply {
            level = HttpLoggingInterceptor.Level.BASIC
        }

    private fun isAnonymousAuthPath(encodedPath: String): Boolean =
        anonymousAuthPaths.any { encodedPath.endsWith(it) }

    private fun Response.priorResponseCount(): Int {
        var count = 1
        var prior = priorResponse
        while (prior != null) {
            count++
            prior = prior.priorResponse
        }
        return count
    }
}
