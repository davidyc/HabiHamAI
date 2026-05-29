package com.habiham.tracking.util

import java.time.DayOfWeek
import java.time.LocalDate
import java.time.ZoneOffset
const val CUSTOM_PERIOD_PRESET = "custom"

data class DateRange(val from: String, val to: String)

/** Последние 7 дней включая сегодня (как «Неделя» в вебе). */
fun rollingWeekRange(): DateRange = DateRange(
    from = isoDateDaysAgo(6),
    to = todayIso(),
)

fun currentCalendarMonthUtc(): DateRange {
    val today = LocalDate.now(ZoneOffset.UTC)
    val first = today.withDayOfMonth(1)
    val last = today.withDayOfMonth(today.lengthOfMonth())
    return DateRange(from = first.toString(), to = last.toString())
}

/** Ближайшие предстоящие выходные (сб–вс) в UTC. */
fun weekendRangeUtc(): DateRange {
    val today = LocalDate.now(ZoneOffset.UTC)
    val dow = today.dayOfWeek
    val daysUntilSaturday = when (dow) {
        DayOfWeek.SATURDAY -> 0L
        DayOfWeek.SUNDAY -> 6L
        else -> (DayOfWeek.SATURDAY.value - dow.value).toLong()
    }
    val saturday = today.plusDays(daysUntilSaturday)
    val sunday = saturday.plusDays(1)
    return DateRange(from = saturday.toString(), to = sunday.toString())
}

/** Пресеты аналитики привычек: 7 / 14 / 30 дней. */
fun applyHabitPeriodPreset(days: Long): DateRange = DateRange(
    from = isoDateDaysAgo((days - 1).coerceAtLeast(0)),
    to = todayIso(),
)

fun applyTodoPeriodPreset(preset: String): DateRange? = when (preset) {
    "all" -> null
    "month" -> currentCalendarMonthUtc()
    "week" -> rollingWeekRange()
    "3days" -> DateRange(from = isoDateDaysAgo(2), to = todayIso())
    "weekend" -> weekendRangeUtc()
    else -> null
}
