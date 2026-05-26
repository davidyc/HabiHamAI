package com.habiham.mobile.data.prefs

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.combine

class UserSessionManager(
    private val sessionStore: SessionStore,
    private val apiSettingsStore: ApiSettingsStore,
) {
    val sessionFlow: Flow<StoredSession?> = combine(
        sessionStore.accessTokenFlow,
        apiSettingsStore.apiBaseUrlFlow,
    ) { token, apiBaseUrl ->
        if (token.isNullOrBlank() || apiBaseUrl.isBlank()) {
            null
        } else {
            StoredSession(accessToken = token, apiBaseUrl = apiBaseUrl)
        }
    }
}
