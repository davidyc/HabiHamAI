package com.habiham.mobile.ui.workouts

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.habiham.mobile.data.model.WorkoutSessionDto
import com.habiham.mobile.data.prefs.StoredSession
import com.habiham.mobile.data.repository.WorkoutsRepository
import com.habiham.mobile.domain.CurrentWorkout
import com.habiham.mobile.domain.CurrentWorkoutExercise
import com.habiham.mobile.domain.CurrentWorkoutSet
import com.habiham.mobile.domain.currentWorkoutFromProgram
import com.habiham.mobile.domain.currentWorkoutFromScratch
import com.habiham.mobile.domain.toCurrentWorkout
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
data class ActiveWorkoutUiState(
    val programs: List<WorkoutSessionDto> = emptyList(),
    val activeSession: WorkoutSessionDto? = null,
    val selectedProgramCode: String? = null,
    val currentWorkout: CurrentWorkout? = null,
    val isEditorOpen: Boolean = false,
    val isLoading: Boolean = false,
    val isSaving: Boolean = false,
    val error: String? = null,
)

class ActiveWorkoutViewModel(
    private val session: StoredSession,
    private val workoutsRepository: WorkoutsRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow(ActiveWorkoutUiState())
    val uiState: StateFlow<ActiveWorkoutUiState> = _uiState.asStateFlow()

    init {
        refresh()
    }

    fun refresh() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            workoutsRepository.loadSessions(session, includeHistory = false).fold(
                onSuccess = { list ->
                    val programs = list.filter {
                        it.sessionCode.orEmpty().startsWith("program::", ignoreCase = true)
                    }
                    val active = list.find {
                        it.sessionCode.orEmpty().startsWith("workout::", ignoreCase = true) &&
                            it.isActive == true
                    }
                    _uiState.update { state ->
                        val mergedCurrent = when {
                            state.isEditorOpen && state.currentWorkout != null -> state.currentWorkout
                            else -> state.currentWorkout ?: active?.toCurrentWorkout()
                        }
                        state.copy(
                            isLoading = false,
                            programs = programs,
                            activeSession = active,
                            currentWorkout = mergedCurrent,
                        )
                    }
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isLoading = false, error = err.message) }
                },
            )
        }
    }

    fun selectProgram(sessionCode: String?) {
        _uiState.update { it.copy(selectedProgramCode = sessionCode?.takeIf { c -> c.isNotBlank() }) }
    }

    fun startFromSelectedProgram() {
        val code = _uiState.value.selectedProgramCode ?: return
        val program = _uiState.value.programs.find { it.sessionCode == code } ?: return
        openEditor(currentWorkoutFromProgram(program))
    }

    fun startFromScratch() {
        openEditor(currentWorkoutFromScratch())
    }

    fun openEditor(workout: CurrentWorkout? = null) {
        val resolved = workout
            ?: _uiState.value.currentWorkout
            ?: _uiState.value.activeSession?.toCurrentWorkout()
        if (resolved != null) {
            _uiState.update { it.copy(currentWorkout = resolved, isEditorOpen = true, error = null) }
        }
    }

    fun closeEditor() {
        _uiState.update { it.copy(isEditorOpen = false) }
    }

    fun updateWorkoutField(day: String? = null, date: String? = null, notes: String? = null) {
        _uiState.update { state ->
            val w = state.currentWorkout ?: return@update state
            state.copy(
                currentWorkout = w.copy(
                    day = day ?: w.day,
                    date = date ?: w.date,
                    notes = notes ?: w.notes,
                ),
            )
        }
    }

    fun addExercise() {
        _uiState.update { state ->
            val w = state.currentWorkout ?: return@update state
            state.copy(
                currentWorkout = w.copy(
                    exercises = w.exercises + CurrentWorkoutExercise(),
                ),
            )
        }
    }

    fun updateExercise(localId: String, name: String? = null, meta: String? = null) {
        _uiState.update { state ->
            val w = state.currentWorkout ?: return@update state
            state.copy(
                currentWorkout = w.copy(
                    exercises = w.exercises.map { ex ->
                        if (ex.localId != localId) ex
                        else ex.copy(
                            name = name ?: ex.name,
                            meta = meta ?: ex.meta,
                        )
                    },
                ),
            )
        }
    }

    fun removeExercise(localId: String) {
        _uiState.update { state ->
            val w = state.currentWorkout ?: return@update state
            state.copy(
                currentWorkout = w.copy(
                    exercises = w.exercises.filter { it.localId != localId },
                ),
            )
        }
    }

    fun addSet(exerciseLocalId: String) {
        _uiState.update { state ->
            val w = state.currentWorkout ?: return@update state
            state.copy(
                currentWorkout = w.copy(
                    exercises = w.exercises.map { ex ->
                        if (ex.localId != exerciseLocalId) ex
                        else {
                            val newSet = ex.sets.lastOrNull()?.copy() ?: CurrentWorkoutSet()
                            ex.copy(sets = ex.sets + newSet)
                        }
                    },
                ),
            )
        }
    }

    fun updateSet(exerciseLocalId: String, setIndex: Int, weight: String? = null, reps: String? = null, rpe: String? = null) {
        _uiState.update { state ->
            val w = state.currentWorkout ?: return@update state
            state.copy(
                currentWorkout = w.copy(
                    exercises = w.exercises.map { ex ->
                        if (ex.localId != exerciseLocalId) ex
                        else ex.copy(
                            sets = ex.sets.mapIndexed { idx, set ->
                                if (idx != setIndex) set
                                else set.copy(
                                    weight = weight ?: set.weight,
                                    reps = reps ?: set.reps,
                                    rpe = rpe ?: set.rpe,
                                )
                            },
                        )
                    },
                ),
            )
        }
    }

    fun removeSet(exerciseLocalId: String, setIndex: Int) {
        _uiState.update { state ->
            val w = state.currentWorkout ?: return@update state
            state.copy(
                currentWorkout = w.copy(
                    exercises = w.exercises.map { ex ->
                        if (ex.localId != exerciseLocalId) ex
                        else ex.copy(sets = ex.sets.filterIndexed { idx, _ -> idx != setIndex })
                    },
                ),
            )
        }
    }

    fun persist(finish: Boolean) {
        val workout = _uiState.value.currentWorkout ?: return
        if (workout.day.isBlank()) {
            _uiState.update { it.copy(error = "Укажите название тренировки.") }
            return
        }
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            workoutsRepository.upsertWorkout(session, workout.toUpsertRequest(finish)).fold(
                onSuccess = { saved ->
                    _uiState.update { state ->
                        state.copy(
                            isSaving = false,
                            isEditorOpen = !finish,
                            currentWorkout = if (finish) null else saved.toCurrentWorkout(),
                            activeSession = if (finish) null else saved,
                        )
                    }
                    refresh()
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isSaving = false, error = err.message) }
                },
            )
        }
    }
}
