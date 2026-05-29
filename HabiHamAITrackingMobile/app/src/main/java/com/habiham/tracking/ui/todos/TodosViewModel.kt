package com.habiham.tracking.ui.todos

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.habiham.tracking.data.model.TodoItemDto
import com.habiham.tracking.data.model.UserCategoryDto
import com.habiham.tracking.data.prefs.StoredSession
import com.habiham.tracking.data.repository.TrackingRepository
import com.habiham.tracking.domain.CATEGORY_ALL_KEY
import com.habiham.tracking.domain.CategoryFilterOption
import com.habiham.tracking.domain.CategoryGroup
import com.habiham.tracking.domain.TodoSortKey
import com.habiham.tracking.domain.TodoTableSort
import com.habiham.tracking.domain.activeTodoCategoryGroup
import com.habiham.tracking.domain.buildTodoCategoryGroups
import com.habiham.tracking.domain.cycleTodoTableSort
import com.habiham.tracking.domain.sortTodoItems
import com.habiham.tracking.domain.todoCategoryFilterOptions
import com.habiham.tracking.domain.todosAllCategoryGroup
import com.habiham.tracking.util.CUSTOM_PERIOD_PRESET
import com.habiham.tracking.util.applyTodoPeriodPreset
import com.habiham.tracking.util.rollingWeekRange
import com.habiham.tracking.util.todayIso
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

enum class TodoStatusFilter(val apiValue: String) {
    All("all"),
    Open("open"),
    Done("done"),
}

data class TodoStatusFilterOption(
    val filter: TodoStatusFilter,
    val label: String,
)

data class TodosUiState(
    val todos: List<TodoItemDto> = emptyList(),
    val categories: List<UserCategoryDto> = emptyList(),
    val dateFrom: String = rollingWeekRange().from,
    val dateTo: String = rollingWeekRange().to,
    val datePeriodPreset: String = "week",
    val categoryTabKey: String = CATEGORY_ALL_KEY,
    val statusFilter: TodoStatusFilter = TodoStatusFilter.All,
    val tableSort: TodoTableSort = TodoTableSort(),
    val isLoading: Boolean = false,
    val isSaving: Boolean = false,
    val error: String? = null,
    val showCreateDialog: Boolean = false,
    val pendingDeleteTodo: TodoItemDto? = null,
)

class TodosViewModel(
    private val session: StoredSession,
    private val repository: TrackingRepository,
) : ViewModel() {
    private val _uiState = MutableStateFlow(TodosUiState())
    val uiState: StateFlow<TodosUiState> = _uiState.asStateFlow()

    init {
        refresh()
        loadCategories()
    }

    fun todosFilteredByStatus(): List<TodoItemDto> {
        val todos = _uiState.value.todos
        return when (_uiState.value.statusFilter) {
            TodoStatusFilter.All -> todos
            TodoStatusFilter.Open -> todos.filter { it.doneDate.isNullOrBlank() }
            TodoStatusFilter.Done -> todos.filter { !it.doneDate.isNullOrBlank() }
        }
    }

    fun statusFilterOptions(): List<TodoStatusFilterOption> {
        val todos = _uiState.value.todos
        val open = todos.count { it.doneDate.isNullOrBlank() }
        val done = todos.count { !it.doneDate.isNullOrBlank() }
        return listOf(
            TodoStatusFilterOption(TodoStatusFilter.All, "Все (${todos.size})"),
            TodoStatusFilterOption(TodoStatusFilter.Open, "Открытые ($open)"),
            TodoStatusFilterOption(TodoStatusFilter.Done, "Готово ($done)"),
        )
    }

    fun categoryFilterOptions(): List<CategoryFilterOption> {
        val filtered = todosFilteredByStatus()
        return todoCategoryFilterOptions(
            allGroup = todosAllCategoryGroup(filtered),
            groups = buildTodoCategoryGroups(filtered, _uiState.value.categories),
        )
    }

    fun displayTodos(): List<TodoItemDto> {
        val state = _uiState.value
        val filtered = todosFilteredByStatus()
        val group = activeTodoCategoryGroup(filtered, state.categories, state.categoryTabKey)
        val items = group?.items.orEmpty()
        return sortTodoItems(items, state.tableSort)
    }

    fun activeCategoryGroup(): CategoryGroup<TodoItemDto>? {
        val filtered = todosFilteredByStatus()
        return activeTodoCategoryGroup(filtered, _uiState.value.categories, _uiState.value.categoryTabKey)
    }

    fun onCategoryTabChange(key: String) {
        _uiState.update { it.copy(categoryTabKey = key) }
    }

    fun onPeriodPresetChange(preset: String) {
        _uiState.update { it.copy(datePeriodPreset = preset) }
    }

    fun applyPeriodPreset(preset: String) {
        if (preset == CUSTOM_PERIOD_PRESET) return
        val range = applyTodoPeriodPreset(preset)
        _uiState.update {
            it.copy(
                datePeriodPreset = preset,
                dateFrom = range?.from.orEmpty(),
                dateTo = range?.to.orEmpty(),
                error = null,
            )
        }
        refresh()
    }

    fun onDateFromChange(value: String) {
        _uiState.update { it.copy(dateFrom = value, error = null) }
        if (_uiState.value.datePeriodPreset == CUSTOM_PERIOD_PRESET) {
            refresh()
        }
    }

    fun onDateToChange(value: String) {
        _uiState.update { it.copy(dateTo = value, error = null) }
        if (_uiState.value.datePeriodPreset == CUSTOM_PERIOD_PRESET) {
            refresh()
        }
    }

    fun onStatusFilterChange(filter: TodoStatusFilter) {
        _uiState.update {
            val filtered = when (filter) {
                TodoStatusFilter.All -> it.todos
                TodoStatusFilter.Open -> it.todos.filter { t -> t.doneDate.isNullOrBlank() }
                TodoStatusFilter.Done -> it.todos.filter { t -> !t.doneDate.isNullOrBlank() }
            }
            val validKeys = buildTodoCategoryGroups(filtered, it.categories)
                .map { g -> g.tabKey }
                .toSet() + CATEGORY_ALL_KEY
            val categoryKey = if (it.categoryTabKey in validKeys || filtered.isEmpty()) {
                it.categoryTabKey
            } else {
                CATEGORY_ALL_KEY
            }
            it.copy(statusFilter = filter, categoryTabKey = categoryKey)
        }
    }

    fun cycleTableSort(key: TodoSortKey) {
        _uiState.update { it.copy(tableSort = cycleTodoTableSort(it.tableSort, key)) }
    }

    fun refresh() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            val state = _uiState.value
            val from = state.dateFrom.takeIf { it.isNotBlank() }
            val to = state.dateTo.takeIf { it.isNotBlank() }
            repository.loadTodos(session, from, to).fold(
                onSuccess = { list ->
                    _uiState.update { s ->
                        val filtered = when (s.statusFilter) {
                            TodoStatusFilter.All -> list
                            TodoStatusFilter.Open -> list.filter { it.doneDate.isNullOrBlank() }
                            TodoStatusFilter.Done -> list.filter { !it.doneDate.isNullOrBlank() }
                        }
                        val validKeys = buildTodoCategoryGroups(filtered, s.categories)
                            .map { g -> g.tabKey }
                            .toSet() + CATEGORY_ALL_KEY
                        val categoryKey = if (s.categoryTabKey in validKeys || filtered.isEmpty()) {
                            s.categoryTabKey
                        } else {
                            CATEGORY_ALL_KEY
                        }
                        s.copy(todos = list, categoryTabKey = categoryKey, isLoading = false)
                    }
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isLoading = false, error = err.message) }
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

    fun requestDelete(todo: TodoItemDto) {
        _uiState.update { it.copy(pendingDeleteTodo = todo) }
    }

    fun cancelDelete() {
        _uiState.update { it.copy(pendingDeleteTodo = null) }
    }

    fun confirmDelete() {
        val todo = _uiState.value.pendingDeleteTodo ?: return
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true) }
            repository.deleteTodo(session, todo.id).fold(
                onSuccess = {
                    _uiState.update { it.copy(pendingDeleteTodo = null, isSaving = false) }
                    refresh()
                },
                onFailure = { err ->
                    _uiState.update {
                        it.copy(isSaving = false, error = err.message, pendingDeleteTodo = null)
                    }
                },
            )
        }
    }

    fun createTodo(title: String, dueDate: String?, categoryId: String?) {
        if (title.isBlank()) {
            _uiState.update { it.copy(error = "Укажите название задачи.") }
            return
        }
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            repository.createTodo(session, title, dueDate, categoryId).fold(
                onSuccess = {
                    _uiState.update { it.copy(isSaving = false, showCreateDialog = false) }
                    refresh()
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isSaving = false, error = err.message) }
                },
            )
        }
    }

    fun toggleDone(todo: TodoItemDto) {
        viewModelScope.launch {
            _uiState.update { it.copy(isSaving = true, error = null) }
            val newDone = todo.doneDate.isNullOrBlank()
            repository.setTodoDone(
                session = session,
                todoId = todo.id,
                isDone = newDone,
                date = if (newDone) todayIso() else null,
            ).fold(
                onSuccess = {
                    _uiState.update { it.copy(isSaving = false) }
                    refresh()
                },
                onFailure = { err ->
                    _uiState.update { it.copy(isSaving = false, error = err.message) }
                },
            )
        }
    }
}
