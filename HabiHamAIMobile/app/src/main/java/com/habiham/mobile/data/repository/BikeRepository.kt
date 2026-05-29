package com.habiham.mobile.data.repository

import android.content.ContentResolver
import android.net.Uri
import com.habiham.mobile.data.api.AuthenticatedApiFactory
import com.habiham.mobile.data.api.toUserMessage
import com.habiham.mobile.data.model.BikeActivityDetailDto
import com.habiham.mobile.data.model.BikeActivitySummaryDto
import com.habiham.mobile.data.prefs.StoredSession
import okhttp3.MediaType.Companion.toMediaTypeOrNull
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody

data class BikeListFilters(
    val from: String? = null,
    val to: String? = null,
    val sport: String = "Biking",
)

class BikeRepository(
    private val apiFactory: AuthenticatedApiFactory,
) {
    suspend fun list(
        session: StoredSession,
        filters: BikeListFilters,
    ): Result<List<BikeActivitySummaryDto>> = apiCall(session) { api ->
        api.listBikeActivities(
            from = filters.from?.takeIf { it.isNotBlank() },
            to = filters.to?.takeIf { it.isNotBlank() },
            sport = filters.sport,
        )
    }

    suspend fun getDetail(
        session: StoredSession,
        id: String,
    ): Result<BikeActivityDetailDto> = apiCall(session) { api ->
        api.getBikeActivity(id = id, trackpointLimit = 5000)
    }

    suspend fun delete(session: StoredSession, id: String): Result<Unit> = runCatching {
        val api = apiFactory.create(session.apiBaseUrl)
        val response = api.deleteBikeActivity(id)
        if (!response.isSuccessful && response.code() != 204) {
            throw retrofit2.HttpException(response)
        }
    }.fold(
        onSuccess = { Result.success(Unit) },
        onFailure = { Result.failure(Exception(it.toUserMessage(), it)) },
    )

    suspend fun importTcx(
        session: StoredSession,
        contentResolver: ContentResolver,
        uri: Uri,
        displayName: String?,
    ): Result<BikeActivitySummaryDto> = runCatching {
        val api = apiFactory.create(session.apiBaseUrl)
        val bytes = contentResolver.openInputStream(uri)?.use { it.readBytes() }
            ?: error("Не удалось прочитать файл.")
        val fileName = displayName?.takeIf { it.endsWith(".tcx", ignoreCase = true) }
            ?: displayName?.let { "$it.tcx" }
            ?: "activity.tcx"
        val body = bytes.toRequestBody("application/xml".toMediaTypeOrNull())
        val part = MultipartBody.Part.createFormData("file", fileName, body)
        api.importBikeTcx(part)
    }.fold(
        onSuccess = { Result.success(it) },
        onFailure = { Result.failure(Exception(it.toUserMessage(), it)) },
    )

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
