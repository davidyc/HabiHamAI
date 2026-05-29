package com.habiham.tracking.domain

import com.habiham.tracking.data.model.HabitOverviewDto
import com.habiham.tracking.util.isoDateRange
import com.habiham.tracking.util.todayIso
import java.time.Instant

object HabitCheckinStatus {
    const val Partial = "partial"
    const val Done = "done"
    const val Failed = "failed"
}

private val statusCycle = listOf<String?>(null, HabitCheckinStatus.Partial, HabitCheckinStatus.Done, HabitCheckinStatus.Failed)

fun nextHabitCheckinStatus(current: String?): String? {
    val normalized = current?.lowercase()?.takeIf { it.isNotBlank() }
    val idx = statusCycle.indexOf(normalized)
    val nextIdx = if (idx < 0) 0 else (idx + 1) % statusCycle.size
    return statusCycle[nextIdx]
}

fun habitStatusLabel(status: String?): String = when (status?.lowercase()) {
    HabitCheckinStatus.Partial -> "частично"
    HabitCheckinStatus.Done -> "выполнено"
    HabitCheckinStatus.Failed -> "провалено"
    else -> "без отметки"
}

fun habitCreatedIsoDate(habit: HabitOverviewDto): String? {
    val raw = habit.createdAtUtc ?: return null
    return runCatching {
        Instant.parse(raw).atZone(java.time.ZoneOffset.UTC).toLocalDate().toString()
    }.getOrNull()
}

fun resolveHabitStatusForDate(
    statusMap: Map<String, String>,
    habit: HabitOverviewDto,
    date: String,
    useTodayFromHabit: Boolean,
): String? {
    statusMap[date]?.let { return it }
    if (!useTodayFromHabit || date != todayIso()) return null
    return habit.todayStatus?.lowercase()?.takeIf { it.isNotBlank() }
}

fun habitDisplayDates(
    habit: HabitOverviewDto,
    filterFrom: String,
    filterTo: String,
): List<String> {
    if (filterFrom.isBlank() || filterTo.isBlank()) return emptyList()
    val created = habitCreatedIsoDate(habit)
    val effectiveFrom = if (created != null && created > filterFrom) created else filterFrom
    if (effectiveFrom > filterTo) return emptyList()
    return isoDateRange(effectiveFrom, filterTo)
}
