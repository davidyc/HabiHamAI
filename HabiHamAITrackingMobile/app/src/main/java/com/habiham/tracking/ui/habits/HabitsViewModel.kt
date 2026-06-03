package com.habiham.tracking.ui.habits

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.habiham.tracking.data.model.HabitOverviewDto
import com.habiham.tracking.data.model.UserCategoryDto
import com.habiham.tracking.data.prefs.StoredSession
import com.habiham.tracking.data.repository.TrackingRepository
import com.habiham.tracking.domain.CATEGORY_ALL_KEY
import com.habiham.tracking.domain.CategoryFilterOption
import com.habiham.tracking.domain.CategoryGroup
import com.habiham.tracking.domain.activeHabitCategoryGroup
import com.habiham.tracking.domain.activeHabitsOnly
import com.habiham.tracking.domain.buildHabitCategoryGroups
import com.habiham.tracking.domain.habitCategoryFilterOptions
import com.habiham.tracking.domain.habitsAllCategoryGroup
import com.habiham.tracking.domain.masteredHabitsInView
import com.habiham.tracking.domain.habitCreatedIsoDate
import com.habiham.tracking.domain.nextHabitCheckinStatus
import com.habiham.tracking.domain.resolveHabitStatusForDate
import com.habiham.tracking.util.CUSTOM_PERIOD_PRESET
import com.habiham.tracking.util.applyHabitPeriodPreset
import com.habiham.tracking.util.rollingWeekRange
import com.habiham.tracking.util.todayIso
import com.habiham.tracking.util.yesterdayIso
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class HabitsUiState(
    val habits: List<HabitOverviewDto> = emptyList(),
    val checkinsByHabitId: Map<String, Map<String, String>> = emptyMap(),
    val categories: List<UserCategoryDto> = emptyList(),
    val dateFrom: String = rollingWeekRange().from,
    val dateTo: String = rollingWeekRange().to,
    val datePeriodPreset: String = "7",
    val categoryTabKey: String = CATEGORY_ALL_KEY,
    val isLoading: Boolean = false,
    val isSaving: Boolean = false,
    val error: String? = null,
    val showCreateDialog: Boolean = false,
    val editingHabit: HabitOverviewDto? = null,
    val pendingDeleteHabit: HabitOverviewDto? = null,
)

class HabitsViewModel(
    private val session: StoredSession,
    private val repository: TrackingRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow(HabitsUiState())
    val uiState: StateFlow<HabitsUiState> = _uiState.asStateFlow()

    init {
        loadOverview()
        loadCategories()
    }

    fun categoryFilterOptions(): List<CategoryFilterOption> {
        val state = _uiState.value
        val active = activeHabitsOnly(state.habits)
        return habitCategoryFilterOptions(
            allGroup = habitsAllCategoryGroup(active),
            groups = buildHabitCategoryGroups(active, state.categories),
        )
    }

    fun activeCategoryGroup(): CategoryGroup<HabitOverviewDto>? {
        val state = _uiState.value
        return activeHabitCategoryGroup(
            activeHabitsOnly(state.habits),
            state.categories,
            state.categoryTabKey,
        )
    }

    fun masteredHabitsForView(): List<HabitOverviewDto> {
        val state = _uiState.value
        return masteredHabitsInView(state.habits, state.categoryTabKey)
    }

    fun onCategoryTabChange(key: String) {
        _uiState.update { it.copy(categoryTabKey = key) }
        reloadCheckins()
    }

    fun onPeriodPresetChange(preset: String) {
        _uiState.update { it.copy(datePeriodPreset = preset) }
    }

    fun applyPeriodPreset(preset: String) {
        if (preset == CUSTOM_PERIOD_PRESET) return
        val range = applyHabitPeriodPreset(preset.toLongOrNull() ?: 7L)
        _uiState.update {
            it.copy(
                datePeriodPreset = preset,
                dateFrom = range.from,
                dateTo = range.to,
                error = null,
            )
        }
        reloadCheckins()
    }

    fun onDateFromChange(value: String) {
        _uiState.update { it.copy(dateFrom = value, error = null) }
        if (_uiState.value.datePeriodPreset == CUSTOM_PERIOD_PRESET) {
            reloadCheckins()
        }
    }

    fun onDateToChange(value: String) {
        _uiState.update { it.copy(dateTo = value, error = null) }
        if (_uiState.value.datePeriodPreset == CUSTOM_PERIOD_PRESET) {
            reloadCheckins()
        }
    }

    fun refresh() {
        loadOverview()
    }

    private fun loadOverview() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            repository.loadHabitsOverview(session).fold(
                onSuccess = { habits ->
                    _uiState.update { state ->
                        val active = activeHabitsOnly(habits)
                        val validKeys = buildHabitCategoryGroups(active, state.categories)
                            .map { it.tabKey }
                            .toSet() + CATEGORY_ALL_KEY
                        val categoryKey = if (state.categoryTabKey in validKeys || habits.isEmpty()) {
                            state.categoryTabKey
                        } else {
                            CATEGORY_ALL_KEY
                        }
                        state.copy(habits = habits, categoryTabKey = categoryKey, isLoading = false)
                    }
                    reloadCheckins()
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isLoading = false, error = err.message) }
                },
            )
        }
    }

    private fun reloadCheckins() {
        viewModelScope.launch {
            val state = _uiState.value
            val active = activeHabitCategoryGroup(
                activeHabitsOnly(state.habits),
                state.categories,
                state.categoryTabKey,
            )?.items.orEmpty()
            val mastered = masteredHabitsInView(state.habits, state.categoryTabKey)
            val visible = active + mastered
            if (visible.isEmpty()) {
                _uiState.update { it.copy(checkinsByHabitId = emptyMap()) }
                return@launch
            }
            repository.loadCheckinsForHabits(
                session = session,
                habits = visible,
                from = state.dateFrom,
                to = state.dateTo,
            ).fold(
                onSuccess = { map ->
                    _uiState.update { it.copy(checkinsByHabitId = map, error = null) }
                },
                onFailure = { err ->
                    _uiState.update { it.copy(error = err.message) }
                },
            )
        }
    }

    private fun loadCategories() {
        viewModelScope.launch {
            repository.loadCategories(session).onSuccess { list ->
                _uiState.update { it.copy(categories = list.filter { c -> c.isActive }) }
            }
        }
    }

    fun openCreateDialog() {
        _uiState.update { it.copy(showCreateDialog = true, error = null) }
    }

    fun closeCreateDialog() {
        _uiState.update { it.copy(showCreateDialog = false) }
    }

    fun openEditDialog(habit: HabitOverviewDto) {
        _uiState.update { it.copy(editingHabit = habit, error = null) }
    }

    fun closeEditDialog() {
        _uiState.update { it.copy(editingHabit = null) }
    }

    fun requestDelete(habit: HabitOverviewDto) {
        _uiState.update { it.copy(pendingDeleteHabit = habit) }
    }

    fun cancelDelete() {
        _uiState.update { it.copy(pendingDeleteHabit = null) }
    }

    fun confirmDelete() {
        val habit = _uiState.value.pendingDeleteHabit ?: return
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            repository.deleteHabit(session, habit.id).fold(
                onSuccess = {
                    _uiState.update { it.copy(pendingDeleteHabit = null, isSaving = false) }
                    loadOverview()
                },
                onFailure = { err ->
                    _uiState.update {
                        it.copy(isSaving = false, error = err.message, pendingDeleteHabit = null)
                    }
                },
            )
        }
    }

    fun createHabit(name: String, categoryId: String?, daysToMaster: Int) {
        if (name.isBlank()) {
            _uiState.update { it.copy(error = "Укажите название привычки.") }
            return
        }
        if (daysToMaster !in 0..999) {
            _uiState.update { it.copy(error = "Дней до освоения: от 0 до 999.") }
            return
        }
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            repository.createHabit(session, name, categoryId, daysToMaster).fold(
                onSuccess = {
                    _uiState.update { it.copy(isSaving = false, showCreateDialog = false) }
                    loadOverview()
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isSaving = false, error = err.message) }
                },
            )
        }
    }

    fun updateHabit(name: String, categoryId: String?, daysToMaster: Int) {
        val habit = _uiState.value.editingHabit ?: return
        if (name.isBlank()) {
            _uiState.update { it.copy(error = "Укажите название привычки.") }
            return
        }
        if (daysToMaster !in 0..999) {
            _uiState.update { it.copy(error = "Дней до освоения: от 0 до 999.") }
            return
        }
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            repository.updateHabit(session, habit.id, name, categoryId, daysToMaster).fold(
                onSuccess = {
                    _uiState.update { it.copy(isSaving = false, editingHabit = null) }
                    loadOverview()
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isSaving = false, error = err.message) }
                },
            )
        }
    }

    fun cycleCheckin(habit: HabitOverviewDto, date: String) {
        viewModelScope.launch {
            val habitKey = habit.id
            val statusMap = _uiState.value.checkinsByHabitId[habitKey].orEmpty()
            val today = todayIso()
            val current = resolveHabitStatusForDate(
                statusMap = statusMap,
                habit = habit,
                date = date,
                useTodayFromHabit = date == today,
            )
            val next = nextHabitCheckinStatus(current)
            _uiState.update { it.copy(isSaving = true, error = null) }
            val result: Result<Unit> = if (next == null) {
                repository.deleteHabitCheckin(session, habit.id, date)
            } else {
                repository.upsertHabitCheckin(session, habit.id, date, next).map { }
            }
            result.fold(
                onSuccess = {
                    _uiState.update { it.copy(isSaving = false) }
                    loadOverview()
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isSaving = false, error = err.message) }
                },
            )
        }
    }

    fun canMarkYesterday(habit: HabitOverviewDto): Boolean {
        val created = habitCreatedIsoDate(habit) ?: return true
        return created <= yesterdayIso()
    }
}
