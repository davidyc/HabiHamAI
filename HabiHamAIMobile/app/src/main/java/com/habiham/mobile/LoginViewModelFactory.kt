package com.habiham.mobile

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.habiham.mobile.data.repository.AuthRepository
import com.habiham.mobile.ui.login.LoginViewModel

class LoginViewModelFactory(
    private val authRepository: AuthRepository,
    private val onLoggedIn: () -> Unit,
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(LoginViewModel::class.java)) {
            return LoginViewModel(authRepository, onLoggedIn) as T
        }
        throw IllegalArgumentException("Unknown ViewModel: ${modelClass.name}")
    }
}
