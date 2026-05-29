package com.habiham.tracking

import android.app.Application
import com.habiham.tracking.data.api.AuthenticatedApiFactory
import com.habiham.tracking.data.auth.SessionRefreshCoordinator
import com.habiham.tracking.data.prefs.ApiSettingsStore
import com.habiham.tracking.data.prefs.CredentialsStore
import com.habiham.tracking.data.prefs.SessionStore
import com.habiham.tracking.data.prefs.UserSessionManager
import com.habiham.tracking.data.repository.AuthRepository
import com.habiham.tracking.data.repository.TrackingRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob

class TrackingApplication : Application() {
    private val applicationScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    lateinit var apiSettingsStore: ApiSettingsStore
        private set

    lateinit var sessionStore: SessionStore
        private set

    lateinit var credentialsStore: CredentialsStore
        private set

    lateinit var userSessionManager: UserSessionManager
        private set

    lateinit var authRepository: AuthRepository
        private set

    lateinit var trackingRepository: TrackingRepository
        private set

    override fun onCreate() {
        super.onCreate()

        apiSettingsStore = ApiSettingsStore(this, BuildConfig.DEFAULT_API_BASE_URL)
        sessionStore = SessionStore(this)
        credentialsStore = CredentialsStore(this)
        authRepository = AuthRepository(sessionStore, apiSettingsStore, credentialsStore)

        val refreshCoordinator = SessionRefreshCoordinator(authRepository)
        val apiFactory = AuthenticatedApiFactory(sessionStore, refreshCoordinator)
        trackingRepository = TrackingRepository(apiFactory)

        userSessionManager = UserSessionManager(
            sessionStore = sessionStore,
            apiSettingsStore = apiSettingsStore,
            authRepository = authRepository,
            appScope = applicationScope,
        )
    }
}
