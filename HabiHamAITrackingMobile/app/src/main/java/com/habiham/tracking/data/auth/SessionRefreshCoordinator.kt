package com.habiham.tracking.data.auth

import com.habiham.tracking.data.repository.AuthRepository
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

class SessionRefreshCoordinator(
    private val authRepository: AuthRepository,
) {
    private val mutex = Mutex()

    suspend fun refreshSession(): Boolean = mutex.withLock {
        authRepository.reloginWithStoredCredentials().isSuccess
    }
}
