package com.habiham.tracking.domain

import com.habiham.tracking.data.model.HabitOverviewDto
import com.habiham.tracking.data.model.TodoItemDto
import com.habiham.tracking.util.isoDateRange
import com.habiham.tracking.util.todayIso

data class HabitDailyRate(
    val date: String,
    val done: Int,
    val total: Int,
    val pct: Int,
)

data class HabitPeriodAnalytics(
    val habitCount: Int,
    val total: Int,
    val none: Int,
    val partial: Int,
    val done: Int,
    val failed: Int,
    val completionPct: Int,
    val bestStreak: Int,
    val dailyRates: List<HabitDailyRate>,
    val periodLabel: String,
)

data class TodoDayCount(
    val date: String,
    val count: Int,
)

data class TodoPeriodAnalytics(
    val total: Int,
    val open: Int,
    val done: Int,
    val overdue: Int,
    val completionPct: Int,
    val completedByDay: List<TodoDayCount>,
    val maxDayCount: Int,
    val periodLabel: String,
)

fun computeHabitPeriodAnalytics(
    habits: List<HabitOverviewDto>,
    checkinsByHabitId: Map<String, Map<String, String>>,
    filterFrom: String,
    filterTo: String,
): HabitPeriodAnalytics? {
    if (habits.isEmpty()) return null

    var none = 0
    var partial = 0
    var done = 0
    var failed = 0
    var bestStreak = 0
    val dailyMap = linkedMapOf<String, Pair<Int, Int>>()

    for (habit in habits) {
        bestStreak = maxOf(bestStreak, habit.currentStreakDays ?: 0)
        val statusMap = checkinsByHabitId[habit.id] ?: emptyMap()
        val dates = habitDisplayDates(habit, filterFrom, filterTo)
        for (date in dates) {
            val status = resolveHabitStatusForDate(
                statusMap = statusMap,
                habit = habit,
                date = date,
                useTodayFromHabit = date == todayIso(),
            )
            when (status?.lowercase()) {
                HabitCheckinStatus.Partial -> partial++
                HabitCheckinStatus.Done -> done++
                HabitCheckinStatus.Failed -> failed++
                else -> none++
            }
            val (dDone, dTotal) = dailyMap.getOrDefault(date, 0 to 0)
            val nextDone = dDone + if (status?.lowercase() == HabitCheckinStatus.Done) 1 else 0
            dailyMap[date] = nextDone to (dTotal + 1)
        }
    }

    val total = none + partial + done + failed
    val completionPct = if (total > 0) ((done * 100.0) / total).toInt() else 0
    val dailyRates = dailyMap.entries
        .sortedBy { it.key }
        .map { (date, counts) ->
            val pct = if (counts.second > 0) ((counts.first * 100.0) / counts.second).toInt() else 0
            HabitDailyRate(date = date, done = counts.first, total = counts.second, pct = pct)
        }

    val periodLabel = if (filterFrom.isNotBlank() && filterTo.isNotBlank()) {
        "$filterFrom — $filterTo"
    } else {
        ""
    }

    return HabitPeriodAnalytics(
        habitCount = habits.size,
        total = total,
        none = none,
        partial = partial,
        done = done,
        failed = failed,
        completionPct = completionPct,
        bestStreak = bestStreak,
        dailyRates = dailyRates,
        periodLabel = periodLabel,
    )
}

fun computeTodoPeriodAnalytics(
    todos: List<TodoItemDto>,
    filterFrom: String,
    filterTo: String,
): TodoPeriodAnalytics? {
    if (todos.isEmpty()) return null

    val today = todayIso()
    var open = 0
    var done = 0
    var overdue = 0

    for (todo in todos) {
        val doneDate = todo.doneDate?.takeIf { it.isNotBlank() }
        if (doneDate != null) {
            done++
        } else {
            open++
            if (isTodoOverdue(todo)) overdue++
        }
    }

    val total = todos.size
    val completionPct = if (total > 0) ((done * 100.0) / total).toInt() else 0

    var from = filterFrom
    var to = filterTo.ifBlank { today }
    if (from.isBlank()) {
        val doneDates = todos.mapNotNull { it.doneDate?.take(10)?.takeIf { d -> d.isNotBlank() } }
        from = doneDates.minOrNull() ?: today
    }
    if (to.isBlank()) to = today
    val rangeFrom = if (from <= to) from else to
    val rangeTo = if (from <= to) to else from

    val completedByDay = isoDateRange(rangeFrom, rangeTo).map { date ->
        val count = todos.count { (it.doneDate?.take(10) ?: "") == date }
        TodoDayCount(date = date, count = count)
    }
    val maxDayCount = maxOf(1, completedByDay.maxOfOrNull { it.count } ?: 0)

    val periodLabel = if (filterFrom.isNotBlank() && filterTo.isNotBlank()) {
        "$filterFrom — $filterTo"
    } else {
        ""
    }

    return TodoPeriodAnalytics(
        total = total,
        open = open,
        done = done,
        overdue = overdue,
        completionPct = completionPct,
        completedByDay = completedByDay,
        maxDayCount = maxDayCount,
        periodLabel = periodLabel,
    )
}
