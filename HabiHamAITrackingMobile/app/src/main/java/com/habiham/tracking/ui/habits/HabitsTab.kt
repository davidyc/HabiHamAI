package com.habiham.tracking.ui.habits

import androidx.compose.foundation.horizontalScroll
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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import com.habiham.tracking.domain.computeHabitPeriodAnalytics
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.habiham.tracking.data.model.HabitOverviewDto
import com.habiham.tracking.data.model.UserCategoryDto
import com.habiham.tracking.domain.habitDisplayDates
import com.habiham.tracking.domain.resolveHabitStatusForDate
import com.habiham.tracking.ui.components.CategoryFilterRow
import com.habiham.tracking.ui.components.CategoryGroupHeader
import com.habiham.tracking.ui.components.DatePeriodFilter
import com.habiham.tracking.ui.components.HabiHamListCard
import com.habiham.tracking.ui.components.HabitStatusDot
import com.habiham.tracking.ui.components.HabitPeriodAnalyticsPanel
import com.habiham.tracking.ui.components.HabitStatusLegend
import com.habiham.tracking.ui.components.SectionTitle
import com.habiham.tracking.ui.components.habihamTextFieldColors
import com.habiham.tracking.ui.components.scrollWithIme
import com.habiham.tracking.ui.filter.HABIT_DATE_PERIOD_OPTIONS
import com.habiham.tracking.util.todayIso
import com.habiham.tracking.util.yesterdayIso
import androidx.compose.material3.FilterChip
import androidx.compose.material3.OutlinedTextField

@OptIn(ExperimentalLayoutApi::class)
@Composable
fun HabitsTab(
    viewModel: HabitsViewModel,
    modifier: Modifier = Modifier,
) {
    val state by viewModel.uiState.collectAsState()
    val activeGroup = viewModel.activeCategoryGroup()
    val categoryOptions = viewModel.categoryFilterOptions()
    val displayHabits = activeGroup?.items.orEmpty()
    val habitAnalytics = remember(state.checkinsByHabitId, displayHabits, state.dateFrom, state.dateTo) {
        computeHabitPeriodAnalytics(
            habits = displayHabits,
            checkinsByHabitId = state.checkinsByHabitId,
            filterFrom = state.dateFrom,
            filterTo = state.dateTo,
        )
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
                    text = "Привычки",
                    subtitle = "Отмечайте сегодня и вчера по клику. Остальной период — только просмотр.",
                )
                Button(
                    onClick = viewModel::openCreateDialog,
                    enabled = !state.isSaving,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Text("Добавить привычку")
                }
                Spacer(Modifier.height(12.dp))
                Text(
                    "Период",
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
                    options = HABIT_DATE_PERIOD_OPTIONS,
                    onApplyPreset = viewModel::applyPeriodPreset,
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
                Spacer(Modifier.height(8.dp))
                HabitStatusLegend()
                if (habitAnalytics != null) {
                    Spacer(Modifier.height(12.dp))
                    HabitPeriodAnalyticsPanel(summary = habitAnalytics)
                }
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
            state.isLoading && state.habits.isEmpty() -> {
                item {
                    Box(
                        Modifier.fillMaxWidth().height(120.dp),
                        contentAlignment = Alignment.Center,
                    ) {
                        CircularProgressIndicator()
                    }
                }
            }
            state.habits.isEmpty() -> {
                item {
                    Text(
                        "Привычек пока нет. Нажмите «Добавить привычку».",
                        modifier = Modifier.padding(horizontal = 16.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
            displayHabits.isEmpty() -> {
                item {
                    Text(
                        "Нет привычек в выбранной категории.",
                        modifier = Modifier.padding(horizontal = 16.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
            else -> {
                items(displayHabits, key = { it.id }) { habit ->
                    HabitCard(
                        habit = habit,
                        statusMap = state.checkinsByHabitId[habit.id].orEmpty(),
                        dateFrom = state.dateFrom,
                        dateTo = state.dateTo,
                        showCategory = activeGroup?.showCategoryColumn == true,
                        canMarkYesterday = viewModel.canMarkYesterday(habit),
                        onCycleYesterday = { viewModel.cycleCheckin(habit, yesterdayIso()) },
                        onCycleToday = { viewModel.cycleCheckin(habit, todayIso()) },
                        onDelete = { viewModel.requestDelete(habit) },
                        modifier = Modifier.padding(horizontal = 16.dp),
                    )
                }
            }
        }
    }

    if (state.showCreateDialog) {
        CreateHabitDialog(
            categories = state.categories,
            isSaving = state.isSaving,
            onDismiss = viewModel::closeCreateDialog,
            onConfirm = viewModel::createHabit,
        )
    }

    state.pendingDeleteHabit?.let { habit ->
        AlertDialog(
            onDismissRequest = viewModel::cancelDelete,
            title = { Text("Удалить привычку?") },
            text = { Text(habit.name) },
            confirmButton = {
                TextButton(onClick = viewModel::confirmDelete) { Text("Удалить") }
            },
            dismissButton = {
                TextButton(onClick = viewModel::cancelDelete) { Text("Отмена") }
            },
        )
    }
}

@Composable
private fun HabitCard(
    habit: HabitOverviewDto,
    statusMap: Map<String, String>,
    dateFrom: String,
    dateTo: String,
    showCategory: Boolean,
    canMarkYesterday: Boolean,
    onCycleYesterday: () -> Unit,
    onCycleToday: () -> Unit,
    onDelete: () -> Unit,
    modifier: Modifier = Modifier,
) {
    val today = todayIso()
    val yesterday = yesterdayIso()
    val todayStatus = resolveHabitStatusForDate(statusMap, habit, today, useTodayFromHabit = true)
    val yesterdayStatus = if (canMarkYesterday) {
        resolveHabitStatusForDate(statusMap, habit, yesterday, useTodayFromHabit = false)
    } else null
    val periodDates = habitDisplayDates(habit, dateFrom, dateTo)

    HabiHamListCard(modifier = modifier) {
        Text(habit.name, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        if (showCategory) {
            Text(
                habit.categoryName ?: "—",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        Text("Серия: ${habit.currentStreakDays} дн.", style = MaterialTheme.typography.bodySmall)
        Spacer(Modifier.height(8.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(16.dp), verticalAlignment = Alignment.CenterVertically) {
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text("Вчера", style = MaterialTheme.typography.labelSmall)
                if (canMarkYesterday) {
                    HabitStatusDot(status = yesterdayStatus, onClick = onCycleYesterday)
                } else {
                    HabitStatusDot(status = null)
                }
            }
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Text("Сегодня", style = MaterialTheme.typography.labelSmall)
                HabitStatusDot(status = todayStatus, onClick = onCycleToday)
            }
        }
        if (periodDates.isNotEmpty()) {
            Spacer(Modifier.height(8.dp))
            Text("Период", style = MaterialTheme.typography.labelSmall)
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .horizontalScroll(rememberScrollState()),
                horizontalArrangement = Arrangement.spacedBy(4.dp),
            ) {
                periodDates.forEach { date ->
                    HabitStatusDot(
                        status = statusMap[date],
                        modifier = Modifier.width(14.dp).height(14.dp),
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
fun CreateHabitDialog(
    categories: List<UserCategoryDto>,
    isSaving: Boolean,
    onDismiss: () -> Unit,
    onConfirm: (name: String, categoryId: String?) -> Unit,
) {
    var name by remember { mutableStateOf("") }
    var selectedCategoryId by remember { mutableStateOf<String?>(null) }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Новая привычка") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    label = { Text("Название") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    colors = habihamTextFieldColors(),
                )
                if (categories.isNotEmpty()) {
                    Text("Категория (необязательно)", style = MaterialTheme.typography.labelSmall)
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
            TextButton(onClick = { onConfirm(name, selectedCategoryId) }, enabled = !isSaving) {
                Text("Сохранить")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text("Отмена") }
        },
    )
}
