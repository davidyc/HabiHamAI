package com.habiham.tracking.data.prefs

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map

private val Context.apiSettingsDataStore: DataStore<Preferences> by preferencesDataStore(
    name = "habiham_tracking_api_settings",
)

class ApiSettingsStore(
    context: Context,
    private val defaultApiBaseUrl: String,
) {
    private val dataStore = context.apiSettingsDataStore

    val apiBaseUrlFlow: Flow<String> = dataStore.data.map { prefs ->
        prefs[Keys.API_BASE_URL].normalizeApiBaseUrlOr(defaultApiBaseUrl)
    }

    suspend fun getApiBaseUrl(): String =
        apiBaseUrlFlow.first().normalizeApiBaseUrlOr(defaultApiBaseUrl)

    suspend fun setApiBaseUrl(url: String) {
        val normalized = url.normalizeApiBaseUrlOr(defaultApiBaseUrl)
        dataStore.edit { prefs ->
            prefs[Keys.API_BASE_URL] = normalized
        }
    }

    private object Keys {
        val API_BASE_URL = stringPreferencesKey("api_base_url")
    }
}

private fun String?.normalizeApiBaseUrlOr(fallback: String): String {
    val trimmed = this?.trim()?.trimEnd('/')?.takeIf { it.isNotEmpty() }
    return trimmed ?: fallback.trim().trimEnd('/')
}
