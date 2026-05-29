package com.habiham.tracking.ui.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.width
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.habiham.tracking.util.CUSTOM_PERIOD_PRESET

data class PeriodPresetOption(
    val value: String,
    val label: String,
)

@OptIn(ExperimentalMaterial3Api::class, ExperimentalLayoutApi::class)
@Composable
fun DatePeriodFilter(
    label: String = "Период",
    preset: String,
    onPresetChange: (String) -> Unit,
    from: String,
    to: String,
    onFromChange: (String) -> Unit,
    onToChange: (String) -> Unit,
    options: List<PeriodPresetOption>,
    onApplyPreset: (String) -> Unit,
    modifier: Modifier = Modifier,
    trailingContent: @Composable () -> Unit = {},
) {
    var expanded by remember { mutableStateOf(false) }
    val selectedLabel = options.find { it.value == preset }?.label ?: "Выберите…"
    val showCustomDates = preset == CUSTOM_PERIOD_PRESET

    Column(modifier = modifier.fillMaxWidth()) {
        Text(label, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Spacer(Modifier.height(4.dp))
        FlowRow(
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            ExposedDropdownMenuBox(
                expanded = expanded,
                onExpandedChange = { expanded = it },
                modifier = Modifier.width(200.dp),
            ) {
                OutlinedTextField(
                    value = selectedLabel,
                    onValueChange = {},
                    readOnly = true,
                    modifier = Modifier
                        .menuAnchor()
                        .fillMaxWidth(),
                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
                    colors = habihamTextFieldColors(),
                    shape = MaterialTheme.shapes.small,
                )
                ExposedDropdownMenu(
                    expanded = expanded,
                    onDismissRequest = { expanded = false },
                ) {
                    options.forEach { opt ->
                        DropdownMenuItem(
                            text = { Text(opt.label) },
                            onClick = {
                                expanded = false
                                onPresetChange(opt.value)
                                if (opt.value != CUSTOM_PERIOD_PRESET) {
                                    onApplyPreset(opt.value)
                                }
                            },
                        )
                    }
                }
            }
            if (showCustomDates) {
                OutlinedTextField(
                    value = from,
                    onValueChange = onFromChange,
                    label = { Text("С") },
                    modifier = Modifier.width(140.dp),
                    singleLine = true,
                    colors = habihamTextFieldColors(),
                    shape = MaterialTheme.shapes.small,
                )
                OutlinedTextField(
                    value = to,
                    onValueChange = onToChange,
                    label = { Text("По") },
                    modifier = Modifier.width(140.dp),
                    singleLine = true,
                    colors = habihamTextFieldColors(),
                    shape = MaterialTheme.shapes.small,
                )
            }
            trailingContent()
        }
    }
}
