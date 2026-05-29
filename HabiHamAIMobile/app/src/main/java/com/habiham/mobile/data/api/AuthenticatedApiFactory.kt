package com.habiham.mobile.data.api

import com.habiham.mobile.data.auth.SessionRefreshCoordinator
import com.habiham.mobile.data.prefs.SessionStore
import kotlinx.coroutines.runBlocking

class AuthenticatedApiFactory(
    private val sessionStore: SessionStore,
    private val refreshCoordinator: SessionRefreshCoordinator,
) {
    fun create(baseUrl: String): HabiHamApi =
        ApiClientFactory.createAuthenticated(
            baseUrl = baseUrl,
            sessionStore = sessionStore,
            refreshCoordinator = refreshCoordinator,
        )
}
