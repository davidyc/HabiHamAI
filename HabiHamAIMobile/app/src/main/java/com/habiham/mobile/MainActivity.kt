package com.habiham.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.habiham.mobile.ui.components.HabiHamScreen
import androidx.lifecycle.viewmodel.compose.viewModel
import com.habiham.mobile.ui.bike.BikeViewModel
import com.habiham.mobile.ui.login.LoginScreen
import com.habiham.mobile.ui.login.LoginViewModel
import com.habiham.mobile.ui.main.MainScreen
import com.habiham.mobile.ui.main.MainViewModelFactory
import com.habiham.mobile.ui.settings.ApiSettingsDialog
import com.habiham.mobile.ui.settings.ApiSettingsViewModel
import com.habiham.mobile.ui.settings.ApiSettingsViewModelFactory
import com.habiham.mobile.ui.theme.HabiHamTheme
import com.habiham.mobile.ui.workouts.ActiveWorkoutViewModel
import com.habiham.mobile.ui.workouts.WorkoutsViewModel
import kotlinx.coroutines.launch

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        installSplashScreen()
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val app = application as HabiHamApplication

        setContent {
            HabiHamTheme {
                HabiHamRoot(app = app)
            }
        }
    }
}

@Composable
private fun HabiHamRoot(app: HabiHamApplication) {
    val sessionNullable by app.userSessionManager.sessionFlow.collectAsStateWithLifecycle(initialValue = null)
    val isBootstrapping by app.userSessionManager.isBootstrappingFlow.collectAsStateWithLifecycle()
    val scope = rememberCoroutineScope()

    var sessionKey by remember { mutableIntStateOf(0) }
    var showApiSettings by remember { mutableStateOf(false) }
    val session = sessionNullable

    if (isBootstrapping) {
        HabiHamScreen {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center,
            ) {
                CircularProgressIndicator()
            }
        }
        return
    }

    val apiSettingsViewModel: ApiSettingsViewModel = viewModel(
        factory = ApiSettingsViewModelFactory(app.apiSettingsStore),
    )

    if (showApiSettings) {
        ApiSettingsDialog(
            viewModel = apiSettingsViewModel,
            onDismiss = { showApiSettings = false },
        )
    }

    if (session == null) {
        val loginViewModel: LoginViewModel = viewModel(
            key = "login-$sessionKey",
            factory = LoginViewModelFactory(
                authRepository = app.authRepository,
                onLoggedIn = { sessionKey += 1 },
            ),
        )
        LoginScreen(
            viewModel = loginViewModel,
            onOpenApiSettings = { showApiSettings = true },
        )
    } else {
        val factory = MainViewModelFactory(
            session = session,
            workoutsRepository = app.workoutsRepository,
            bikeRepository = app.bikeRepository,
        )
        val sessionVmKey = "${session.accessToken.hashCode()}-${session.apiBaseUrl.hashCode()}"
        val workoutsViewModel: WorkoutsViewModel = viewModel(
            key = "workouts-$sessionVmKey",
            factory = factory,
        )
        val activeWorkoutViewModel: ActiveWorkoutViewModel = viewModel(
            key = "active-$sessionVmKey",
            factory = factory,
        )
        val bikeViewModel: BikeViewModel = viewModel(
            key = "bike-$sessionVmKey",
            factory = factory,
        )
        MainScreen(
            workoutsViewModel = workoutsViewModel,
            activeWorkoutViewModel = activeWorkoutViewModel,
            bikeViewModel = bikeViewModel,
            onOpenApiSettings = { showApiSettings = true },
            onLogout = {
                scope.launch {
                    app.authRepository.logout()
                    sessionKey += 1
                }
            },
        )
    }
}
