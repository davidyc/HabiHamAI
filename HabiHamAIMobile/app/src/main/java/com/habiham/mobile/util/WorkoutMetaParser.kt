package com.habiham.mobile.util

import org.json.JSONObject

/**
 * Как parseProgramExerciseMeta в веб-клиенте: meta может быть JSON
 * {"sourceExerciseId":"...","comment":"..."} или обычным текстом.
 */
fun displayExerciseComment(rawMeta: String?): String {
    val raw = rawMeta?.trim().orEmpty()
    if (raw.isEmpty()) return ""

    return runCatching {
        val parsed = JSONObject(raw)
        val hasStructuredKeys =
            parsed.has("sourceExerciseId") || parsed.has("comment")
        if (hasStructuredKeys) {
            parsed.optString("comment", "")
        } else {
            raw
        }
    }.getOrElse {
        raw
    }
}
