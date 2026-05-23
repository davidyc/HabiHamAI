package com.habiham.mobile.util

import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.util.Locale

private val isoDate = DateTimeFormatter.ISO_LOCAL_DATE
private val displayDate = DateTimeFormatter.ofPattern("d MMMM yyyy", Locale("ru"))

fun todayIso(): String = LocalDate.now().format(isoDate)

fun isoDateDaysAgo(days: Long): String = LocalDate.now().minusDays(days).format(isoDate)

fun formatWorkoutDateLabel(iso: String?): String {
    if (iso.isNullOrBlank()) return "—"
    return runCatching {
        LocalDate.parse(iso.take(10), isoDate).format(displayDate)
    }.getOrElse { iso }
}
