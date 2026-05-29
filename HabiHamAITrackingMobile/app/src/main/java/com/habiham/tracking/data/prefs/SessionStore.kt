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

private val Context.dataStore: DataStore<Preferences> by preferencesDataStore(
    name = "habiham_tracking_session",
)

class SessionStore(private val context: Context) {
    val accessTokenFlow: Flow<String?> = context.dataStore.data.map { prefs ->
        prefs[Keys.ACCESS_TOKEN]?.takeIf { it.isNotBlank() }
    }

    suspend fun saveToken(accessToken: String) {
        context.dataStore.edit { prefs ->
            prefs[Keys.ACCESS_TOKEN] = accessToken.trim()
        }
    }

    suspend fun clearToken() {
        context.dataStore.edit { prefs ->
            prefs.remove(Keys.ACCESS_TOKEN)
        }
    }

    suspend fun getAccessToken(): String? = accessTokenFlow.first()

    private object Keys {
        val ACCESS_TOKEN = stringPreferencesKey("access_token")
    }
}

data class StoredSession(
    val accessToken: String,
    val apiBaseUrl: String,
)
