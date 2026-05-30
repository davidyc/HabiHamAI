package com.habiham.tracking.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.habiham.tracking.domain.HabitCheckinStatus
import com.habiham.tracking.domain.HabitPeriodAnalytics
import com.habiham.tracking.domain.TodoPeriodAnalytics

@Composable
fun HabitPeriodAnalyticsPanel(
    summary: HabitPeriodAnalytics?,
    modifier: Modifier = Modifier,
) {
    if (summary == null) return

    AnalyticsPanelShell(modifier = modifier, title = "Аналитика за период", periodLabel = summary.periodLabel) {
        AnalyticsStatsRow {
            AnalyticsStatCard("Привычек", summary.habitCount.toString())
            AnalyticsStatCard("Отметок", summary.total.toString(), hint = "за период")
            AnalyticsStatCard("Выполнено", "${summary.completionPct}%")
            AnalyticsStatCard("Лучшая серия", "${summary.bestStreak} дн.")
        }
        HabitStatusDistributionBar(
            none = summary.none,
            partial = summary.partial,
            done = summary.done,
            failed = summary.failed,
            total = summary.total,
        )
        if (summary.dailyRates.isNotEmpty()) {
            Spacer(Modifier.height(8.dp))
            Text(
                "Доля выполненных привычек по дням (%)",
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(Modifier.height(6.dp))
            Row(
                Modifier
                    .fillMaxWidth()
                    .horizontalScroll(rememberScrollState()),
                horizontalArrangement = Arrangement.spacedBy(4.dp),
                verticalAlignment = Alignment.Bottom,
            ) {
                summary.dailyRates.forEach { day ->
                    AnalyticsBarColumn(
                        label = day.date.takeLast(5),
                        heightFraction = (day.pct.coerceAtLeast(4)) / 100f,
                        barColor = MaterialTheme.colorScheme.primary,
                    )
                }
            }
        }
    }
}

@Composable
fun TodoPeriodAnalyticsPanel(
    summary: TodoPeriodAnalytics?,
    modifier: Modifier = Modifier,
) {
    if (summary == null) return

    AnalyticsPanelShell(modifier = modifier, title = "Аналитика за период", periodLabel = summary.periodLabel) {
        AnalyticsStatsRow {
            AnalyticsStatCard("Всего", summary.total.toString())
            AnalyticsStatCard("Выполнено", summary.done.toString())
            AnalyticsStatCard("Открыто", summary.open.toString())
            AnalyticsStatCard("Просрочено", summary.overdue.toString())
            AnalyticsStatCard("Готово", "${summary.completionPct}%")
        }
        if (summary.completedByDay.any { it.count > 0 }) {
            Spacer(Modifier.height(8.dp))
            Text(
                "Задач отмечено выполненными по дням",
                style = MaterialTheme.typography.labelSmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(Modifier.height(6.dp))
            Row(
                Modifier
                    .fillMaxWidth()
                    .horizontalScroll(rememberScrollState()),
                horizontalArrangement = Arrangement.spacedBy(4.dp),
                verticalAlignment = Alignment.Bottom,
            ) {
                summary.completedByDay.forEach { day ->
                    val fraction = day.count.toFloat() / summary.maxDayCount.toFloat()
                    AnalyticsBarColumn(
                        label = day.date.takeLast(5),
                        heightFraction = fraction.coerceAtLeast(0.04f),
                        barColor = MaterialTheme.colorScheme.tertiary,
                    )
                }
            }
        } else {
            Text(
                "За период нет отмеченных выполненных задач.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@Composable
private fun AnalyticsPanelShell(
    title: String,
    periodLabel: String,
    modifier: Modifier = Modifier,
    content: @Composable () -> Unit,
) {
    Surface(
        modifier = modifier.fillMaxWidth(),
        shape = RoundedCornerShape(12.dp),
        color = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.55f),
    ) {
        Column(Modifier.padding(14.dp)) {
            Text(title, style = MaterialTheme.typography.titleSmall, fontWeight = FontWeight.SemiBold)
            if (periodLabel.isNotBlank()) {
                Spacer(Modifier.height(2.dp))
                Text(
                    periodLabel,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Spacer(Modifier.height(10.dp))
            content()
        }
    }
}

@Composable
private fun AnalyticsStatsRow(content: @Composable RowScope.() -> Unit) {
    Row(
        Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        content()
    }
}

@Composable
private fun RowScope.AnalyticsStatCard(label: String, value: String, hint: String? = null) {
    Column(
        Modifier
            .weight(1f)
            .clip(RoundedCornerShape(10.dp))
            .background(MaterialTheme.colorScheme.surface.copy(alpha = 0.5f))
            .padding(horizontal = 8.dp, vertical = 8.dp),
    ) {
        Text(label, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        hint?.let {
            Text(it, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}

@Composable
private fun HabitStatusDistributionBar(
    none: Int,
    partial: Int,
    done: Int,
    failed: Int,
    total: Int,
) {
    if (total <= 0) return
    val segments = listOf(
        Triple(done, HabitCheckinStatus.Done, "выполнено"),
        Triple(partial, HabitCheckinStatus.Partial, "частично"),
        Triple(failed, HabitCheckinStatus.Failed, "провалено"),
        Triple(none, null, "без отметки"),
    ).filter { it.first > 0 }

    Row(
        Modifier
            .fillMaxWidth()
            .height(14.dp)
            .clip(RoundedCornerShape(8.dp)),
        horizontalArrangement = Arrangement.spacedBy(2.dp),
    ) {
        segments.forEach { (count, status, _) ->
            Box(
                Modifier
                    .weight(count.toFloat())
                    .height(14.dp)
                    .clip(RoundedCornerShape(4.dp))
                    .background(statusColor(status)),
            )
        }
    }
    Spacer(Modifier.height(6.dp))
    Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        segments.forEach { (count, status, label) ->
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                HabitStatusDot(status = status, modifier = Modifier.size(10.dp))
                Text("$label: $count", style = MaterialTheme.typography.labelSmall)
            }
        }
    }
}

@Composable
private fun AnalyticsBarColumn(
    label: String,
    heightFraction: Float,
    barColor: androidx.compose.ui.graphics.Color,
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier.width(26.dp),
    ) {
        Box(
            Modifier
                .height(72.dp)
                .width(18.dp),
            contentAlignment = Alignment.BottomCenter,
        ) {
            Box(
                Modifier
                    .fillMaxWidth()
                    .height((72 * heightFraction).dp.coerceAtLeast(4.dp))
                    .clip(RoundedCornerShape(topStart = 4.dp, topEnd = 4.dp))
                    .background(barColor),
            )
        }
        Spacer(Modifier.height(4.dp))
        Text(label, style = MaterialTheme.typography.labelSmall)
    }
}

private fun statusColor(status: String?): androidx.compose.ui.graphics.Color {
    return when (status?.lowercase()) {
        HabitCheckinStatus.Partial -> androidx.compose.ui.graphics.Color(0xFFD4A017)
        HabitCheckinStatus.Done -> androidx.compose.ui.graphics.Color(0xFF3FB950)
        HabitCheckinStatus.Failed -> androidx.compose.ui.graphics.Color(0xFFE63946)
        else -> androidx.compose.ui.graphics.Color(0xFF484F58)
    }
}
