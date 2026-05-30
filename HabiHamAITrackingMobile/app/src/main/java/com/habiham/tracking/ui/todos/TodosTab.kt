package com.habiham.tracking.ui.todos

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Badge
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import com.habiham.tracking.domain.computeTodoPeriodAnalytics
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.unit.dp
import com.habiham.tracking.data.model.TodoItemDto
import com.habiham.tracking.data.model.UserCategoryDto
import com.habiham.tracking.domain.TodoSortDir
import com.habiham.tracking.domain.isTodoOverdue
import com.habiham.tracking.domain.TodoSortKey
import com.habiham.tracking.ui.components.CategoryFilterRow
import com.habiham.tracking.ui.components.CategoryGroupHeader
import com.habiham.tracking.ui.components.DatePeriodFilter
import com.habiham.tracking.ui.components.HabiHamListCard
import com.habiham.tracking.ui.theme.HabiHamColors
import com.habiham.tracking.ui.components.SectionTitle
import com.habiham.tracking.ui.components.TodoPeriodAnalyticsPanel
import com.habiham.tracking.ui.components.habihamTextFieldColors
import com.habiham.tracking.ui.components.scrollWithIme
import com.habiham.tracking.ui.filter.TODO_DATE_PERIOD_OPTIONS

@OptIn(ExperimentalLayoutApi::class, ExperimentalMaterial3Api::class)
@Composable
fun TodosTab(
    viewModel: TodosViewModel,
    modifier: Modifier = Modifier,
) {
    val state by viewModel.uiState.collectAsState()
    val displayTodos = viewModel.displayTodos()
    val activeGroup = viewModel.activeCategoryGroup()
    val categoryOptions = viewModel.categoryFilterOptions()
    val statusOptions = viewModel.statusFilterOptions()
    val todoAnalytics = remember(displayTodos, state.dateFrom, state.dateTo) {
        computeTodoPeriodAnalytics(
            todos = displayTodos,
            filterFrom = state.dateFrom,
            filterTo = state.dateTo,
        )
    }
    val emptyMessage = when {
        state.todos.isEmpty() -> "Задач пока нет. Нажмите «Добавить задачу»."
        state.statusFilter == TodoStatusFilter.Open -> "Нет открытых задач за выбранный период."
        state.statusFilter == TodoStatusFilter.Done -> "Нет выполненных задач за выбранный период."
        displayTodos.isEmpty() -> "Нет задач по выбранному фильтру."
        else -> null
    }

    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .scrollWithIme(),
        contentPadding = PaddingValues(bottom = 16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        item {
            Column(Modifier.padding(horizontal = 16.dp, vertical = 8.dp)) {
                SectionTitle(
                    text = "Задачи",
                    subtitle = "Отметьте выполнение или добавьте новую задачу.",
                )
                Button(
                    onClick = viewModel::openCreateDialog,
                    enabled = !state.isSaving,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Text("Добавить задачу")
                }
                Spacer(Modifier.height(12.dp))
                Text(
                    "Период задач",
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.SemiBold,
                )
                Spacer(Modifier.height(8.dp))
                DatePeriodFilter(
                    preset = state.datePeriodPreset,
                    onPresetChange = viewModel::onPeriodPresetChange,
                    from = state.dateFrom,
                    to = state.dateTo,
                    onFromChange = viewModel::onDateFromChange,
                    onToChange = viewModel::onDateToChange,
                    options = TODO_DATE_PERIOD_OPTIONS,
                    onApplyPreset = viewModel::applyPeriodPreset,
                    trailingContent = {
                        TodoStatusFilterDropdown(
                            options = statusOptions,
                            selected = state.statusFilter,
                            onSelected = viewModel::onStatusFilterChange,
                        )
                    },
                )
                Spacer(Modifier.height(12.dp))
                CategoryFilterRow(
                    options = categoryOptions,
                    selectedKey = state.categoryTabKey,
                    onSelected = viewModel::onCategoryTabChange,
                )
                activeGroup?.let { group ->
                    if (group.showHeader && group.tabLabel != null) {
                        Spacer(Modifier.height(8.dp))
                        CategoryGroupHeader(
                            title = group.tabLabel,
                            doneCount = group.doneCount,
                            totalCount = group.totalCount,
                        )
                    }
                }
                if (todoAnalytics != null) {
                    Spacer(Modifier.height(12.dp))
                    TodoPeriodAnalyticsPanel(summary = todoAnalytics)
                }
                Spacer(Modifier.height(8.dp))
                TodoSortRow(
                    current = state.tableSort,
                    onSort = viewModel::cycleTableSort,
                )
            }
        }

        state.error?.let { msg ->
            item {
                Text(
                    msg,
                    color = MaterialTheme.colorScheme.error,
                    style = MaterialTheme.typography.bodySmall,
                    modifier = Modifier.padding(horizontal = 16.dp),
                )
            }
        }

        when {
            state.isLoading && state.todos.isEmpty() -> {
                item {
                    Box(
                        Modifier.fillMaxWidth().height(120.dp),
                        contentAlignment = Alignment.Center,
                    ) {
                        CircularProgressIndicator()
                    }
                }
            }
            emptyMessage != null -> {
                item {
                    Text(
                        emptyMessage,
                        modifier = Modifier.padding(horizontal = 16.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
            else -> {
                items(displayTodos, key = { it.id }) { todo ->
                    TodoCard(
                        todo = todo,
                        showCategory = activeGroup?.showCategoryColumn == true,
                        onToggleDone = { viewModel.toggleDone(todo) },
                        onDelete = { viewModel.requestDelete(todo) },
                        modifier = Modifier.padding(horizontal = 16.dp),
                    )
                }
            }
        }
    }

    if (state.showCreateDialog) {
        CreateTodoDialog(
            categories = state.categories,
            isSaving = state.isSaving,
            onDismiss = viewModel::closeCreateDialog,
            onConfirm = viewModel::createTodo,
        )
    }

    state.pendingDeleteTodo?.let { todo ->
        AlertDialog(
            onDismissRequest = viewModel::cancelDelete,
            title = { Text("Удалить задачу?") },
            text = { Text(todo.title) },
            confirmButton = {
                TextButton(onClick = viewModel::confirmDelete) { Text("Удалить") }
            },
            dismissButton = {
                TextButton(onClick = viewModel::cancelDelete) { Text("Отмена") }
            },
        )
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun TodoStatusFilterDropdown(
    options: List<TodoStatusFilterOption>,
    selected: TodoStatusFilter,
    onSelected: (TodoStatusFilter) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    val label = options.find { it.filter == selected }?.label ?: "Статус"

    ExposedDropdownMenuBox(
        expanded = expanded,
        onExpandedChange = { expanded = it },
        modifier = Modifier.fillMaxWidth(),
    ) {
        OutlinedTextField(
            value = label,
            onValueChange = {},
            readOnly = true,
            label = { Text("Статус") },
            modifier = Modifier
                .menuAnchor()
                .fillMaxWidth(),
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
            colors = habihamTextFieldColors(),
            shape = MaterialTheme.shapes.small,
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            options.forEach { opt ->
                DropdownMenuItem(
                    text = { Text(opt.label) },
                    onClick = {
                        expanded = false
                        onSelected(opt.filter)
                    },
                )
            }
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun TodoSortRow(
    current: com.habiham.tracking.domain.TodoTableSort,
    onSort: (TodoSortKey) -> Unit,
) {
    Text("Сортировка", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    Spacer(Modifier.height(4.dp))
    FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        TodoSortKey.entries.forEach { key ->
            val selected = current.key == key
            val arrow = if (selected) {
                if (current.dir == TodoSortDir.Asc) " ↑" else " ↓"
            } else {
                ""
            }
            FilterChip(
                selected = selected,
                onClick = { onSort(key) },
                label = { Text("${key.label}$arrow") },
            )
        }
    }
}

@Composable
private fun TodoCard(
    todo: TodoItemDto,
    showCategory: Boolean,
    onToggleDone: () -> Unit,
    onDelete: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val isDone = !todo.doneDate.isNullOrBlank()
    val overdue = isTodoOverdue(todo)
    HabiHamListCard(
        modifier = modifier,
        border = if (overdue) {
            BorderStroke(1.dp, HabiHamColors.HabitFailed.copy(alpha = 0.85f))
        } else {
            null
        },
        containerColor = if (overdue) {
            HabiHamColors.HabitFailed.copy(alpha = 0.12f)
        } else {
            null
        },
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = isDone, onCheckedChange = { onToggleDone() })
            Column(Modifier.weight(1f)) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    Text(
                        todo.title,
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.SemiBold,
                        textDecoration = if (isDone) TextDecoration.LineThrough else null,
                        modifier = Modifier.weight(1f, fill = false),
                    )
                    if (overdue) {
                        Badge(containerColor = HabiHamColors.HabitFailed) {
                            Text("Просрочено", style = MaterialTheme.typography.labelSmall)
                        }
                    }
                }
                val meta = buildList {
                    if (showCategory) add(todo.categoryName ?: "—")
                    todo.dueDate?.let { due ->
                        add(if (overdue) "дедлайн $due" else "до $due")
                    }
                    if (isDone && todo.doneDate != null) add("выполнено ${todo.doneDate}")
                }.joinToString(" · ")
                if (meta.isNotBlank()) {
                    Text(
                        meta,
                        style = MaterialTheme.typography.bodySmall,
                        color = if (overdue) {
                            HabiHamColors.HabitFailed
                        } else {
                            MaterialTheme.colorScheme.onSurfaceVariant
                        },
                        fontWeight = if (overdue) FontWeight.SemiBold else FontWeight.Normal,
                    )
                }
            }
        }
        Spacer(Modifier.height(8.dp))
        OutlinedButton(onClick = onDelete) { Text("Удалить") }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun CreateTodoDialog(
    categories: List<UserCategoryDto>,
    isSaving: Boolean,
    onDismiss: () -> Unit,
    onConfirm: (title: String, dueDate: String?, categoryId: String?) -> Unit,
) {
    var title by remember { mutableStateOf("") }
    var dueDate by remember { mutableStateOf("") }
    var selectedCategoryId by remember { mutableStateOf<String?>(null) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Новая задача") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = title,
                    onValueChange = { title = it },
                    label = { Text("Название") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    colors = habihamTextFieldColors(),
                )
                OutlinedTextField(
                    value = dueDate,
                    onValueChange = { dueDate = it },
                    label = { Text("Дедлайн (ГГГГ-ММ-ДД)") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    colors = habihamTextFieldColors(),
                )
                if (categories.isNotEmpty()) {
                    FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        FilterChip(
                            selected = selectedCategoryId == null,
                            onClick = { selectedCategoryId = null },
                            label = { Text("Без категории") },
                        )
                        categories.forEach { cat ->
                            FilterChip(
                                selected = selectedCategoryId == cat.id,
                                onClick = { selectedCategoryId = cat.id },
                                label = { Text(cat.name) },
                            )
                        }
                    }
                }
            }
        },
        confirmButton = {
            TextButton(
                onClick = { onConfirm(title, dueDate.takeIf { it.isNotBlank() }, selectedCategoryId) },
                enabled = !isSaving,
            ) {
                Text("Сохранить")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Отмена") }
        },
    )
}
