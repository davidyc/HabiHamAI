package com.habiham.mobile.ui.settings

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp

@Composable
fun ApiSettingsDialog(
    viewModel: ApiSettingsViewModel,
    onDismiss: () -> Unit,
) {
    val state by viewModel.uiState.collectAsState()

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Адрес API") },
        text = {
            Column {
                OutlinedTextField(
                    value = state.url,
                    onValueChange = viewModel::onUrlChange,
                    label = { Text("URL") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    isError = state.error != null,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri),
                )
                state.error?.let { msg ->
                    Spacer(Modifier.height(8.dp))
                    Text(msg, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
                }
                Spacer(Modifier.height(12.dp))
                Text(
                    "По умолчанию: https://habihamai.onrender.com\n" +
                        "Локальный API: http://127.0.0.1:5193 (USB + adb reverse), " +
                        "http://10.0.2.2:5193 (эмулятор), http://IP-ПК:5193 (Wi‑Fi)",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Spacer(Modifier.height(8.dp))
                Text(
                    "Значение сохраняется на устройстве. При смене URL перезапустите запросы (потяните «Обновить»).",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        },
        confirmButton = {
            TextButton(
                onClick = { viewModel.save(onDismiss) },
                enabled = !state.isSaving,
            ) {
                Text("Сохранить")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Отмена")
            }
        },
    )
}
