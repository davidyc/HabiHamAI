package com.habiham.mobile.ui.login

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.habiham.mobile.BuildConfig
import com.habiham.mobile.data.repository.AuthRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class LoginUiState(
    val username: String = "",
    val password: String = "",
    val apiBaseUrl: String = BuildConfig.DEFAULT_API_BASE_URL,
    val isLoading: Boolean = false,
    val error: String? = null,
)

class LoginViewModel(
    private val authRepository: AuthRepository,
    private val onLoggedIn: () -> Unit,
) : ViewModel() {
    private val _uiState = MutableStateFlow(LoginUiState())
    val uiState: StateFlow<LoginUiState> = _uiState.asStateFlow()

    fun onUsernameChange(value: String) {
        _uiState.update { it.copy(username = value, error = null) }
    }

    fun onPasswordChange(value: String) {
        _uiState.update { it.copy(password = value, error = null) }
    }

    fun onApiBaseUrlChange(value: String) {
        _uiState.update { it.copy(apiBaseUrl = value, error = null) }
    }

    fun login() {
        val state = _uiState.value
        if (state.username.isBlank() || state.password.isBlank()) {
            _uiState.update { it.copy(error = "Введите логин и пароль.") }
            return
        }
        if (state.apiBaseUrl.isBlank()) {
            _uiState.update { it.copy(error = "Укажите URL API.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            val result = authRepository.login(
                username = state.username,
                password = state.password,
                apiBaseUrl = state.apiBaseUrl,
            )
            _uiState.update { it.copy(isLoading = false) }
            result.fold(
                onSuccess = { onLoggedIn() },
                onFailure = { err ->
                    _uiState.update { it.copy(error = err.message) }
                },
            )
        }
    }
}
