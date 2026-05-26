package com.habiham.mobile.ui.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.habiham.mobile.data.prefs.ApiSettingsStore
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class ApiSettingsUiState(
    val url: String = "",
    val isSaving: Boolean = false,
    val error: String? = null,
)

class ApiSettingsViewModel(
    private val apiSettingsStore: ApiSettingsStore,
) : ViewModel() {
    private val _uiState = MutableStateFlow(ApiSettingsUiState())
    val uiState: StateFlow<ApiSettingsUiState> = _uiState.asStateFlow()

    init {
        viewModelScope.launch {
            apiSettingsStore.apiBaseUrlFlow.collect { url ->
                _uiState.update { it.copy(url = url, error = null) }
            }
        }
    }

    fun onUrlChange(value: String) {
        _uiState.update { it.copy(url = value, error = null) }
    }

    fun save(onSaved: () -> Unit) {
        val url = _uiState.value.url.trim()
        if (url.isBlank()) {
            _uiState.update { it.copy(error = "Укажите URL API.") }
            return
        }
        if (!url.startsWith("http://") && !url.startsWith("https://")) {
            _uiState.update { it.copy(error = "URL должен начинаться с http:// или https://") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            apiSettingsStore.setApiBaseUrl(url)
            _uiState.update { it.copy(isSaving = false) }
            onSaved()
        }
    }
}
