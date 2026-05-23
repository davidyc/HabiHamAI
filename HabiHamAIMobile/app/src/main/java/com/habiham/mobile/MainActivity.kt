package com.habiham.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.habiham.mobile.ui.bike.BikeViewModel
import com.habiham.mobile.ui.login.LoginScreen
import com.habiham.mobile.ui.login.LoginViewModel
import com.habiham.mobile.ui.main.MainScreen
import com.habiham.mobile.ui.main.MainViewModelFactory
import com.habiham.mobile.ui.theme.HabiHamTheme
import com.habiham.mobile.ui.workouts.ActiveWorkoutViewModel
import com.habiham.mobile.ui.workouts.WorkoutsViewModel
import androidx.compose.runtime.rememberCoroutineScope
import kotlinx.coroutines.launch
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
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
    val sessionNullable by app.sessionStore.sessionFlow.collectAsStateWithLifecycle(initialValue = null)
    val scope = rememberCoroutineScope()

    var sessionKey by remember { mutableIntStateOf(0) }
    val session = sessionNullable

    if (session == null) {
        val loginViewModel: LoginViewModel = viewModel(
            key = "login-$sessionKey",
            factory = LoginViewModelFactory(
                authRepository = app.authRepository,
                onLoggedIn = { sessionKey += 1 },
            ),
        )
        LoginScreen(viewModel = loginViewModel)
    } else {
        val factory = MainViewModelFactory(
            session = session,
            workoutsRepository = app.workoutsRepository,
            bikeRepository = app.bikeRepository,
        )
        val workoutsViewModel: WorkoutsViewModel = viewModel(
            key = "workouts-${session.accessToken.hashCode()}",
            factory = factory,
        )
        val activeWorkoutViewModel: ActiveWorkoutViewModel = viewModel(
            key = "active-${session.accessToken.hashCode()}",
            factory = factory,
        )
        val bikeViewModel: BikeViewModel = viewModel(
            key = "bike-${session.accessToken.hashCode()}",
            factory = factory,
        )
        MainScreen(
            workoutsViewModel = workoutsViewModel,
            activeWorkoutViewModel = activeWorkoutViewModel,
            bikeViewModel = bikeViewModel,
            onLogout = {
                scope.launch {
                    app.authRepository.logout()
                    sessionKey += 1
                }
            },
        )
    }
}
