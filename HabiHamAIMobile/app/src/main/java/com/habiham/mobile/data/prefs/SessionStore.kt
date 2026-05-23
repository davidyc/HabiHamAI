package com.habiham.mobile.data.prefs

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "habiham_session")

class SessionStore(private val context: Context) {
    val sessionFlow: Flow<StoredSession?> = context.dataStore.data.map { prefs ->
        val token = prefs[Keys.ACCESS_TOKEN]
        val baseUrl = prefs[Keys.API_BASE_URL]
        if (token.isNullOrBlank() || baseUrl.isNullOrBlank()) {
            null
        } else {
            StoredSession(accessToken = token, apiBaseUrl = baseUrl)
        }
    }

    suspend fun save(accessToken: String, apiBaseUrl: String) {
        context.dataStore.edit { prefs ->
            prefs[Keys.ACCESS_TOKEN] = accessToken
            prefs[Keys.API_BASE_URL] = apiBaseUrl.trim().trimEnd('/')
        }
    }

    suspend fun updateApiBaseUrl(apiBaseUrl: String) {
        context.dataStore.edit { prefs ->
            prefs[Keys.API_BASE_URL] = apiBaseUrl.trim().trimEnd('/')
        }
    }

    suspend fun clear() {
        context.dataStore.edit { it.clear() }
    }

    private object Keys {
        val ACCESS_TOKEN = stringPreferencesKey("access_token")
        val API_BASE_URL = stringPreferencesKey("api_base_url")
    }
}

data class StoredSession(
    val accessToken: String,
    val apiBaseUrl: String,
)
