package com.habiham.tracking.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.dp
import com.habiham.tracking.domain.HabitCheckinStatus
import com.habiham.tracking.ui.theme.HabiHamColors

@Composable
fun HabitStatusDot(
    status: String?,
    modifier: Modifier = Modifier,
    onClick: (() -> Unit)? = null,
) {
    val shape = RoundedCornerShape(4.dp)
    val color = when (status?.lowercase()) {
        HabitCheckinStatus.Partial -> HabiHamColors.HabitPartial
        HabitCheckinStatus.Done -> HabiHamColors.HabitDone
        HabitCheckinStatus.Failed -> HabiHamColors.HabitFailed
        else -> HabiHamColors.HabitNone
    }
    Box(
        modifier = modifier
            .size(18.dp)
            .clip(shape)
            .then(
                if (onClick != null) Modifier.clickable(onClick = onClick) else Modifier,
            )
            .background(color)
            .border(1.dp, HabiHamColors.PanelBorder, shape),
    )
}

@Composable
fun HabitStatusLegend(modifier: Modifier = Modifier) {
    Row(modifier = modifier, horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        legendItem(null, "нет")
        legendItem(HabitCheckinStatus.Partial, "частично")
        legendItem(HabitCheckinStatus.Done, "выполнено")
        legendItem(HabitCheckinStatus.Failed, "провалено")
    }
}

@Composable
private fun legendItem(status: String?, label: String) {
    Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
        HabitStatusDot(status = status)
        Text(label, style = MaterialTheme.typography.labelSmall)
    }
}
