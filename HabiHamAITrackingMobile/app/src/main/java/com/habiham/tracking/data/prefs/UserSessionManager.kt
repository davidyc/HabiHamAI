package com.habiham.tracking.data.prefs

import com.habiham.tracking.data.repository.AuthRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.launch

class UserSessionManager(
    private val sessionStore: SessionStore,
    private val apiSettingsStore: ApiSettingsStore,
    private val authRepository: AuthRepository,
    appScope: CoroutineScope,
) {
    private val isBootstrapping = MutableStateFlow(true)

    val isBootstrappingFlow: StateFlow<Boolean> = isBootstrapping.asStateFlow()

    val sessionFlow: Flow<StoredSession?> = combine(
        sessionStore.accessTokenFlow,
        apiSettingsStore.apiBaseUrlFlow,
        isBootstrapping,
    ) { token, apiBaseUrl, bootstrapping ->
        if (bootstrapping || token.isNullOrBlank() || apiBaseUrl.isBlank()) {
            null
        } else {
            StoredSession(accessToken = token, apiBaseUrl = apiBaseUrl)
        }
    }

    init {
        appScope.launch {
            authRepository.restoreSessionOnStartup()
            isBootstrapping.value = false
        }
    }
}
