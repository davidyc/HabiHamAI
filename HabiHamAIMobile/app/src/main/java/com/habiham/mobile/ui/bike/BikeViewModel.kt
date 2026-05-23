package com.habiham.mobile.ui.bike

import android.content.ContentResolver
import android.net.Uri
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.habiham.mobile.data.model.BikeActivityDetailDto
import com.habiham.mobile.data.model.BikeActivitySummaryDto
import com.habiham.mobile.data.prefs.StoredSession
import com.habiham.mobile.data.repository.BikeListFilters
import com.habiham.mobile.data.repository.BikeRepository
import com.habiham.mobile.util.isoDateDaysAgo
import com.habiham.mobile.util.todayIso
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class BikeUiState(
    val dateFrom: String = isoDateDaysAgo(30),
    val dateTo: String = todayIso(),
    val activities: List<BikeActivitySummaryDto> = emptyList(),
    val selectedDetail: BikeActivityDetailDto? = null,
    val detailLoading: Boolean = false,
    val pendingDeleteId: String? = null,
    val isLoading: Boolean = false,
    val isImporting: Boolean = false,
    val importMessage: String? = null,
    val error: String? = null,
)

class BikeViewModel(
    private val session: StoredSession,
    private val bikeRepository: BikeRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow(BikeUiState())
    val uiState: StateFlow<BikeUiState> = _uiState.asStateFlow()

    init {
        loadActivities()
    }

    fun onDateFromChange(value: String) {
        _uiState.update { it.copy(dateFrom = value, error = null) }
    }

    fun onDateToChange(value: String) {
        _uiState.update { it.copy(dateTo = value, error = null) }
    }

    fun applyDatePreset(days: Long) {
        val safeDays = days.coerceAtLeast(1)
        _uiState.update {
            it.copy(
                dateFrom = isoDateDaysAgo(safeDays - 1),
                dateTo = todayIso(),
                error = null,
            )
        }
        loadActivities()
    }

    fun loadActivities() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            val filters = BikeListFilters(
                from = _uiState.value.dateFrom,
                to = _uiState.value.dateTo,
            )
            bikeRepository.list(session, filters).fold(
                onSuccess = { list ->
                    _uiState.update { it.copy(isLoading = false, activities = list) }
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isLoading = false, error = err.message) }
                },
            )
        }
    }

    fun importTcx(contentResolver: ContentResolver, uri: Uri, displayName: String?) {
        viewModelScope.launch {
            _uiState.update {
                it.copy(isImporting = true, importMessage = null, error = null)
            }
            bikeRepository.importTcx(session, contentResolver, uri, displayName).fold(
                onSuccess = {
                    _uiState.update {
                        it.copy(
                            isImporting = false,
                            importMessage = "Файл успешно импортирован.",
                        )
                    }
                    loadActivities()
                },
                onFailure = { err ->
                    _uiState.update {
                        it.copy(
                            isImporting = false,
                            importMessage = err.message,
                        )
                    }
                },
            )
        }
    }

    fun openDetail(id: String) {
        viewModelScope.launch {
            _uiState.update {
                it.copy(detailLoading = true, selectedDetail = null, error = null)
            }
            bikeRepository.getDetail(session, id).fold(
                onSuccess = { detail ->
                    _uiState.update {
                        it.copy(detailLoading = false, selectedDetail = detail)
                    }
                },
                onFailure = { err ->
                    _uiState.update {
                        it.copy(detailLoading = false, error = err.message)
                    }
                },
            )
        }
    }

    fun closeDetail() {
        _uiState.update { it.copy(selectedDetail = null, detailLoading = false) }
    }

    fun requestDelete(id: String) {
        _uiState.update { it.copy(pendingDeleteId = id) }
    }

    fun cancelDelete() {
        _uiState.update { it.copy(pendingDeleteId = null) }
    }

    fun confirmDelete() {
        val id = _uiState.value.pendingDeleteId ?: return
        viewModelScope.launch {
            bikeRepository.delete(session, id).fold(
                onSuccess = {
                    _uiState.update {
                        it.copy(
                            pendingDeleteId = null,
                            selectedDetail = it.selectedDetail?.takeUnless { d -> d.id == id },
                        )
                    }
                    loadActivities()
                },
                onFailure = { err ->
                    _uiState.update {
                        it.copy(pendingDeleteId = null, error = err.message)
                    }
                },
            )
        }
    }
}
