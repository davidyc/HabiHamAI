package com.habiham.mobile.ui.workouts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.habiham.mobile.data.model.WorkoutSessionDto
import com.habiham.mobile.data.prefs.StoredSession
import com.habiham.mobile.data.repository.WorkoutHistoryFilters
import com.habiham.mobile.data.repository.WorkoutsRepository
import com.habiham.mobile.util.isoDateDaysAgo
import com.habiham.mobile.util.todayIso
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class WorkoutsUiState(
    val dateFrom: String = isoDateDaysAgo(6),
    val dateTo: String = todayIso(),
    val selectedProgram: String? = null,
    val programOptions: List<String> = emptyList(),
    val sessions: List<WorkoutSessionDto> = emptyList(),
    val selectedSession: WorkoutSessionDto? = null,
    val isLoading: Boolean = false,
    val isLoadingOptions: Boolean = false,
    val error: String? = null,
)

class WorkoutsViewModel(
    private val session: StoredSession,
    private val workoutsRepository: WorkoutsRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow(WorkoutsUiState())
    val uiState: StateFlow<WorkoutsUiState> = _uiState.asStateFlow()

    init {
        refreshProgramOptions()
        loadHistory()
    }

    fun onDateFromChange(value: String) {
        _uiState.update { it.copy(dateFrom = value, error = null) }
    }

    fun onDateToChange(value: String) {
        _uiState.update { it.copy(dateTo = value, error = null) }
    }

    fun onProgramSelected(program: String?) {
        _uiState.update {
            it.copy(
                selectedProgram = program?.takeIf { p -> p.isNotBlank() },
                error = null,
            )
        }
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
        loadHistory()
    }

    fun loadHistory() {
        viewModelScope.launch {
            val filters = currentFilters()
            _uiState.update { it.copy(isLoading = true, error = null) }
            workoutsRepository.loadHistory(session, filters).fold(
                onSuccess = { list ->
                    _uiState.update { it.copy(isLoading = false, sessions = list) }
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isLoading = false, error = err.message) }
                },
            )
        }
    }

    fun refreshProgramOptions() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoadingOptions = true) }
            workoutsRepository.loadProgramOptions(session).fold(
                onSuccess = { options ->
                    _uiState.update { it.copy(isLoadingOptions = false, programOptions = options) }
                },
                onFailure = {
                    _uiState.update { it.copy(isLoadingOptions = false) }
                },
            )
        }
    }

    fun openSession(session: WorkoutSessionDto) {
        _uiState.update { it.copy(selectedSession = session) }
    }

    fun closeSessionDetail() {
        _uiState.update { it.copy(selectedSession = null) }
    }

    private fun currentFilters(): WorkoutHistoryFilters {
        val state = _uiState.value
        return WorkoutHistoryFilters(
            from = state.dateFrom,
            to = state.dateTo,
            program = state.selectedProgram,
        )
    }
}
