package com.habiham.mobile.ui.bike

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.FilterChip
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalBottomSheet
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.rememberModalBottomSheetState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.habiham.mobile.data.model.BikeActivityDetailDto
import com.habiham.mobile.data.model.BikeActivitySummaryDto
import com.habiham.mobile.util.formatBikeDurationSeconds
import com.habiham.mobile.util.formatCalories
import com.habiham.mobile.util.formatDistanceKm
import com.habiham.mobile.util.formatHeartRate
import com.habiham.mobile.ui.components.scrollWithIme
import com.habiham.mobile.util.formatUtcDateTime

@OptIn(ExperimentalLayoutApi::class, androidx.compose.material3.ExperimentalMaterial3Api::class)
@Composable
fun BikeTab(
    viewModel: BikeViewModel,
    modifier: Modifier = Modifier,
) {
    val state by viewModel.uiState.collectAsState()
    val context = LocalContext.current
    val sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true)

    val tcxPicker = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.OpenDocument(),
    ) { uri ->
        if (uri != null) {
            val name = context.contentResolver.queryDisplayName(uri)
            viewModel.importTcx(context.contentResolver, uri, name)
        }
    }

    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .scrollWithIme(),
        contentPadding = PaddingValues(bottom = 16.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp),
    ) {
        item {
            BikeTabHeader(
                state = state,
                onImportTcx = { tcxPicker.launch(arrayOf("application/xml", "text/xml", "*/*")) },
                onDateFromChange = viewModel::onDateFromChange,
                onDateToChange = viewModel::onDateToChange,
                onApplyPreset = viewModel::applyDatePreset,
                onApplyFilters = viewModel::loadActivities,
            )
        }

        if (!state.error.isNullOrBlank()) {
            item {
                Text(
                    state.error!!,
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(horizontal = 16.dp),
                    style = MaterialTheme.typography.bodySmall,
                )
            }
        }

        when {
            state.isLoading && state.activities.isEmpty() -> {
                item {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(160.dp),
                        contentAlignment = Alignment.Center,
                    ) {
                        CircularProgressIndicator()
                    }
                }
            }
            state.activities.isEmpty() -> {
                item {
                    Text(
                        "Нет записей по выбранному периоду.",
                        modifier = Modifier.padding(horizontal = 16.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
            else -> {
                items(state.activities, key = { it.id }) { row ->
                    BikeActivityCard(
                        row = row,
                        onOpen = { viewModel.openDetail(row.id) },
                        onDelete = { viewModel.requestDelete(row.id) },
                        modifier = Modifier.padding(horizontal = 16.dp),
                    )
                }
            }
        }
    }

    if (state.pendingDeleteId != null) {
        AlertDialog(
            onDismissRequest = viewModel::cancelDelete,
            title = { Text("Удалить поездку?") },
            confirmButton = {
                TextButton(onClick = viewModel::confirmDelete) { Text("Удалить") }
            },
            dismissButton = {
                TextButton(onClick = viewModel::cancelDelete) { Text("Отмена") }
            },
        )
    }

    if (state.selectedDetail != null || state.detailLoading) {
        ModalBottomSheet(
            onDismissRequest = viewModel::closeDetail,
            sheetState = sheetState,
        ) {
            BikeDetailSheet(
                detail = state.selectedDetail,
                loading = state.detailLoading,
            )
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun BikeTabHeader(
    state: BikeUiState,
    onImportTcx: () -> Unit,
    onDateFromChange: (String) -> Unit,
    onDateToChange: (String) -> Unit,
    onApplyPreset: (Long) -> Unit,
    onApplyFilters: () -> Unit,
) {
    Column(modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)) {
        Text(
            "Велотренировки (TCX)",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.SemiBold,
        )
        Text(
            "Импорт TCX (Zepp и др.). На сервере сохраняются только разобранные данные, спорт Biking.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        Spacer(Modifier.height(8.dp))
        Button(
            onClick = onImportTcx,
            enabled = !state.isImporting,
            modifier = Modifier.fillMaxWidth(),
        ) {
            if (state.isImporting) {
                CircularProgressIndicator(strokeWidth = 2.dp, modifier = Modifier.height(18.dp))
            } else {
                Text("Загрузить TCX")
            }
        }
        state.importMessage?.let { msg ->
            Spacer(Modifier.height(6.dp))
            Text(
                msg,
                style = MaterialTheme.typography.bodySmall,
                color = if (msg.contains("успешно", ignoreCase = true)) {
                    MaterialTheme.colorScheme.primary
                } else {
                    MaterialTheme.colorScheme.error
                },
            )
        }
        Spacer(Modifier.height(12.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            OutlinedTextField(
                value = state.dateFrom,
                onValueChange = onDateFromChange,
                label = { Text("С") },
                modifier = Modifier.weight(1f),
                singleLine = true,
            )
            OutlinedTextField(
                value = state.dateTo,
                onValueChange = onDateToChange,
                label = { Text("По") },
                modifier = Modifier.weight(1f),
                singleLine = true,
            )
        }
        Spacer(Modifier.height(8.dp))
        FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            listOf("Неделя" to 7L, "Месяц" to 30L, "90 дн." to 90L).forEach { (label, days) ->
                FilterChip(
                    selected = false,
                    onClick = { onApplyPreset(days) },
                    label = { Text(label) },
                )
            }
        }
        Spacer(Modifier.height(8.dp))
        Button(
            onClick = onApplyFilters,
            enabled = !state.isLoading,
            modifier = Modifier.fillMaxWidth(),
        ) {
            Text("Применить фильтры")
        }
    }
}

@Composable
private fun BikeActivityCard(
    row: BikeActivitySummaryDto,
    onOpen: () -> Unit,
    onDelete: () -> Unit,
    modifier: Modifier = Modifier,
) {
    Card(
        modifier = modifier
            .fillMaxWidth()
            .clickable(onClick = onOpen),
    ) {
        Column(Modifier.padding(16.dp)) {
            Text(
                row.notes?.ifBlank { "Поездка" } ?: "Поездка",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.SemiBold,
            )
            Text(formatUtcDateTime(row.startTimeUtc), style = MaterialTheme.typography.bodyMedium)
            Spacer(Modifier.height(4.dp))
            Text(
                "${formatBikeDurationSeconds(row.totalSeconds)} · ${formatDistanceKm(row.distanceMeters)} · ${formatCalories(row.calories)} ккал",
                style = MaterialTheme.typography.bodySmall,
            )
            Text(
                "ЧСС ср/макс: ${formatHeartRate(row.averageHeartRateBpm, row.maxHeartRateBpm)} · точек: ${row.trackpointCount}",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Spacer(Modifier.height(8.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedButton(onClick = onOpen) { Text("Маршрут") }
                OutlinedButton(onClick = onDelete) { Text("Удалить") }
            }
        }
    }
}

@Composable
private fun BikeDetailSheet(
    detail: BikeActivityDetailDto?,
    loading: Boolean,
) {
    Column(
        Modifier
            .fillMaxWidth()
            .padding(horizontal = 20.dp)
            .padding(bottom = 32.dp),
    ) {
        Text("Маршрут", style = MaterialTheme.typography.headlineSmall)
        if (loading) {
            Spacer(Modifier.height(16.dp))
            CircularProgressIndicator()
        } else if (detail != null) {
            Spacer(Modifier.height(8.dp))
            Text(
                "${detail.notes ?: detail.sport ?: "Поездка"} · ${formatUtcDateTime(detail.startTimeUtc)}",
                style = MaterialTheme.typography.bodyMedium,
            )
            Text(
                "${formatBikeDurationSeconds(detail.totalSeconds)} · ${formatDistanceKm(detail.distanceMeters)}",
                style = MaterialTheme.typography.bodySmall,
            )
            val shown = detail.trackpoints.size
            val total = detail.trackpointCount
            if (total > shown) {
                Text(
                    "На карте $shown из $total точек",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Spacer(Modifier.height(12.dp))
            if (detail.trackpoints.any { it.latitude != null && it.longitude != null }) {
                BikeTrackMap(trackpoints = detail.trackpoints)
            } else {
                Text("Нет координат для отображения карты.")
            }
            HorizontalDivider(Modifier.padding(vertical = 12.dp))
            Text(
                "ЧСС: ${formatHeartRate(detail.averageHeartRateBpm, detail.maxHeartRateBpm)}",
                style = MaterialTheme.typography.bodySmall,
            )
        }
    }
}

private fun android.content.ContentResolver.queryDisplayName(uri: android.net.Uri): String? {
    val cursor = query(uri, arrayOf(android.provider.OpenableColumns.DISPLAY_NAME), null, null, null)
    cursor?.use {
        if (it.moveToFirst()) {
            val idx = it.getColumnIndex(android.provider.OpenableColumns.DISPLAY_NAME)
            if (idx >= 0) return it.getString(idx)
        }
    }
    return null
}
