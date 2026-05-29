package com.habiham.tracking

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
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
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.habiham.tracking.ui.components.HabiHamScreen
import com.habiham.tracking.ui.habits.HabitsViewModel
import com.habiham.tracking.ui.login.LoginScreen
import com.habiham.tracking.ui.login.LoginViewModel
import com.habiham.tracking.ui.main.MainScreen
import com.habiham.tracking.ui.main.MainViewModelFactory
import com.habiham.tracking.ui.settings.ApiSettingsDialog
import com.habiham.tracking.ui.settings.ApiSettingsViewModel
import com.habiham.tracking.ui.settings.ApiSettingsViewModelFactory
import com.habiham.tracking.ui.theme.HabiHamTheme
import com.habiham.tracking.ui.todos.TodosViewModel
import kotlinx.coroutines.launch

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        installSplashScreen()
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val app = application as TrackingApplication

        setContent {
            HabiHamTheme {
                TrackingRoot(app = app)
            }
        }
    }
}

@Composable
private fun TrackingRoot(app: TrackingApplication) {
    val sessionNullable by app.userSessionManager.sessionFlow.collectAsStateWithLifecycle(initialValue = null)
    val isBootstrapping by app.userSessionManager.isBootstrappingFlow.collectAsStateWithLifecycle()
    val scope = rememberCoroutineScope()

    var sessionKey by remember { mutableIntStateOf(0) }
    var showApiSettings by remember { mutableStateOf(false) }
    val session = sessionNullable

    if (isBootstrapping) {
        HabiHamScreen {
            Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
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
            trackingRepository = app.trackingRepository,
        )
        val sessionVmKey = "${session.accessToken.hashCode()}-${session.apiBaseUrl.hashCode()}"
        val habitsViewModel: HabitsViewModel = viewModel(
            key = "habits-$sessionVmKey",
            factory = factory,
        )
        val todosViewModel: TodosViewModel = viewModel(
            key = "todos-$sessionVmKey",
            factory = factory,
        )
        MainScreen(
            habitsViewModel = habitsViewModel,
            todosViewModel = todosViewModel,
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
