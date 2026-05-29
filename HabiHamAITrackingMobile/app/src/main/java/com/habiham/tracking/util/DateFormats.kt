package com.habiham.tracking.util

import java.time.LocalDate
import java.time.format.DateTimeFormatter

private val isoDate = DateTimeFormatter.ISO_LOCAL_DATE

fun todayIso(): String = LocalDate.now().format(isoDate)

fun isoDateDaysAgo(days: Long): String = LocalDate.now().minusDays(days).format(isoDate)

fun isoDateRange(from: String, to: String): List<String> {
    var current = LocalDate.parse(from.take(10), isoDate)
    val end = LocalDate.parse(to.take(10), isoDate)
    val result = mutableListOf<String>()
    while (!current.isAfter(end)) {
        result.add(current.format(isoDate))
        current = current.plusDays(1)
    }
    return result
}

fun yesterdayIso(): String = isoDateDaysAgo(1)
