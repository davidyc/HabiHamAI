package com.habiham.tracking.data.api

import com.habiham.tracking.data.auth.SessionRefreshCoordinator
import com.habiham.tracking.data.prefs.SessionStore

class AuthenticatedApiFactory(
    private val sessionStore: SessionStore,
    private val refreshCoordinator: SessionRefreshCoordinator,
) {
    fun create(baseUrl: String): TrackingApi =
        ApiClientFactory.createAuthenticated(
            baseUrl = baseUrl,
            sessionStore = sessionStore,
            refreshCoordinator = refreshCoordinator,
        )
}
