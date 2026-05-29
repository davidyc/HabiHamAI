package com.habiham.tracking.ui.filter

import com.habiham.tracking.ui.components.PeriodPresetOption
import com.habiham.tracking.util.CUSTOM_PERIOD_PRESET

val HABIT_DATE_PERIOD_OPTIONS = listOf(
    PeriodPresetOption("7", "7 дней"),
    PeriodPresetOption("14", "14 дней"),
    PeriodPresetOption("30", "30 дней"),
    PeriodPresetOption(CUSTOM_PERIOD_PRESET, "Указать период"),
)

val TODO_DATE_PERIOD_OPTIONS = listOf(
    PeriodPresetOption("all", "Все"),
    PeriodPresetOption("month", "Месяц"),
    PeriodPresetOption("week", "Неделя"),
    PeriodPresetOption("3days", "3 дня"),
    PeriodPresetOption("weekend", "Выходные"),
    PeriodPresetOption(CUSTOM_PERIOD_PRESET, "Указать период"),
)
