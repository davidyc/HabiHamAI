package com.habiham.mobile

import android.app.Application
import com.habiham.mobile.data.api.AuthenticatedApiFactory
import com.habiham.mobile.data.auth.SessionRefreshCoordinator
import com.habiham.mobile.data.prefs.ApiSettingsStore
import com.habiham.mobile.data.prefs.CredentialsStore
import com.habiham.mobile.data.prefs.SessionStore
import com.habiham.mobile.data.prefs.UserSessionManager
import com.habiham.mobile.data.repository.AuthRepository
import com.habiham.mobile.data.repository.BikeRepository
import com.habiham.mobile.data.repository.WorkoutsRepository
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import org.osmdroid.config.Configuration
import java.io.File

class HabiHamApplication : Application() {
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

    lateinit var workoutsRepository: WorkoutsRepository
        private set

    lateinit var bikeRepository: BikeRepository
        private set

    override fun onCreate() {
        super.onCreate()
        Configuration.getInstance().userAgentValue = packageName
        val osmBase = File(cacheDir, "osmdroid").apply { mkdirs() }
        Configuration.getInstance().osmdroidBasePath = osmBase
        Configuration.getInstance().osmdroidTileCache = File(osmBase, "tiles").apply { mkdirs() }

        apiSettingsStore = ApiSettingsStore(this, BuildConfig.DEFAULT_API_BASE_URL)
        sessionStore = SessionStore(this)
        credentialsStore = CredentialsStore(this)
        authRepository = AuthRepository(sessionStore, apiSettingsStore, credentialsStore)

        val refreshCoordinator = SessionRefreshCoordinator(authRepository)
        val apiFactory = AuthenticatedApiFactory(sessionStore, refreshCoordinator)

        userSessionManager = UserSessionManager(
            sessionStore = sessionStore,
            apiSettingsStore = apiSettingsStore,
            authRepository = authRepository,
            appScope = applicationScope,
        )
        workoutsRepository = WorkoutsRepository(apiFactory)
        bikeRepository = BikeRepository(apiFactory)

        applicationScope.launch {
            migrateLegacyApiUrlIfNeeded()
        }
    }

    private suspend fun migrateLegacyApiUrlIfNeeded() {
        if (!sessionStore.hasLegacyApiBaseUrl()) return
        val legacy = sessionStore.consumeLegacyApiBaseUrl() ?: return
        val current = apiSettingsStore.getApiBaseUrl()
        if (current == BuildConfig.DEFAULT_API_BASE_URL) {
            apiSettingsStore.setApiBaseUrl(legacy)
        }
    }
}
