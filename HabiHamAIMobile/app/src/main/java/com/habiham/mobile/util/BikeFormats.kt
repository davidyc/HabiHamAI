package com.habiham.mobile.util

import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale
import kotlin.math.roundToInt

private val displayDateTime = DateTimeFormatter.ofPattern("d MMM yyyy, HH:mm", Locale("ru"))
    .withZone(ZoneId.systemDefault())

fun formatUtcDateTime(iso: String?): String {
    if (iso.isNullOrBlank()) return "—"
    return runCatching {
        displayDateTime.format(Instant.parse(iso))
    }.getOrElse { iso }
}

fun formatBikeDurationSeconds(sec: Double?): String {
    if (sec == null || !sec.isFinite()) return "—"
    val s = sec.roundToInt()
    val h = s / 3600
    val m = (s % 3600) / 60
    val r = s % 60
    return if (h > 0) {
        "$h:${m.toString().padStart(2, '0')}:${r.toString().padStart(2, '0')}"
    } else {
        "$m:${r.toString().padStart(2, '0')}"
    }
}

fun formatDistanceKm(meters: Double?): String {
    if (meters == null || !meters.isFinite()) return "—"
    return "%.2f км".format(Locale.US, meters / 1000.0)
}

fun formatCalories(cal: Double?): String {
    if (cal == null || !cal.isFinite()) return "—"
    return cal.roundToInt().toString()
}

fun formatHeartRate(avg: Int?, max: Int?): String {
    val a = avg?.toString() ?: "—"
    val m = max?.toString() ?: "—"
    return "$a / $m"
}
