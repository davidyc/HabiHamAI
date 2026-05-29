package com.habiham.tracking.ui.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.habiham.tracking.data.prefs.ApiSettingsStore

class ApiSettingsViewModelFactory(
    private val apiSettingsStore: ApiSettingsStore,
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(ApiSettingsViewModel::class.java)) {
            return ApiSettingsViewModel(apiSettingsStore) as T
        }
        throw IllegalArgumentException("Unknown ViewModel: ${modelClass.name}")
    }
}
