package com.habiham.mobile.ui.login



import androidx.lifecycle.ViewModel

import androidx.lifecycle.viewModelScope

import com.habiham.mobile.data.repository.AuthRepository

import kotlinx.coroutines.flow.MutableStateFlow

import kotlinx.coroutines.flow.StateFlow

import kotlinx.coroutines.flow.asStateFlow

import kotlinx.coroutines.flow.update

import kotlinx.coroutines.launch



enum class AuthMode {

    Login,

    Register,

}



data class AuthUiState(

    val mode: AuthMode = AuthMode.Login,

    val username: String = "",

    val password: String = "",

    val isLoading: Boolean = false,

    val error: String? = null,

    val successMessage: String? = null,

)



class LoginViewModel(

    private val authRepository: AuthRepository,

    private val onLoggedIn: () -> Unit,

) : ViewModel() {

    private val _uiState = MutableStateFlow(AuthUiState())

    val uiState: StateFlow<AuthUiState> = _uiState.asStateFlow()

    init {
        authRepository.peekSavedUsername()?.let { savedUsername ->
            _uiState.update { it.copy(username = savedUsername) }
        }
    }

    fun setMode(mode: AuthMode) {

        _uiState.update {

            it.copy(mode = mode, error = null, successMessage = null)

        }

    }



    fun onUsernameChange(value: String) {

        _uiState.update { it.copy(username = value, error = null, successMessage = null) }

    }



    fun onPasswordChange(value: String) {

        _uiState.update { it.copy(password = value, error = null, successMessage = null) }

    }



    fun login() {

        val state = _uiState.value

        if (state.username.isBlank() || state.password.isBlank()) {

            _uiState.update { it.copy(error = "Введите логин и пароль.") }

            return

        }



        viewModelScope.launch {

            _uiState.update { it.copy(isLoading = true, error = null, successMessage = null) }

            val result = authRepository.login(

                username = state.username,

                password = state.password,

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



    fun register() {

        val state = _uiState.value

        if (state.username.isBlank() || state.password.isBlank()) {

            _uiState.update { it.copy(error = "Введите логин и пароль.") }

            return

        }

        if (state.password.length < 6) {

            _uiState.update { it.copy(error = "Пароль должен быть не короче 6 символов.") }

            return

        }



        viewModelScope.launch {

            _uiState.update { it.copy(isLoading = true, error = null, successMessage = null) }

            val result = authRepository.register(

                username = state.username,

                password = state.password,

            )

            _uiState.update { it.copy(isLoading = false) }

            result.fold(

                onSuccess = { message ->

                    _uiState.update {

                        it.copy(

                            mode = AuthMode.Login,

                            password = "",

                            error = null,

                            successMessage = "$message Войдите с созданным аккаунтом.",

                        )

                    }

                },

                onFailure = { err ->

                    _uiState.update { it.copy(error = err.message) }

                },

            )

        }

    }



    fun submit() {

        when (_uiState.value.mode) {

            AuthMode.Login -> login()

            AuthMode.Register -> register()

        }

    }

}

