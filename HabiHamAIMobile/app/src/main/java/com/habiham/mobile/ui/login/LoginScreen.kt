package com.habiham.mobile.ui.login

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.habiham.mobile.ui.components.HabiHamBrandTitle
import com.habiham.mobile.ui.components.HabiHamContentCard
import com.habiham.mobile.ui.components.HabiHamScreen
import com.habiham.mobile.ui.components.HabiHamSegmentTabRow
import com.habiham.mobile.ui.components.SectionTitle
import com.habiham.mobile.ui.components.habihamTextFieldColors
import com.habiham.mobile.ui.theme.HabiHamColors

@Composable
fun LoginScreen(
    viewModel: LoginViewModel,
    onOpenApiSettings: () -> Unit,
) {
    val state by viewModel.uiState.collectAsState()
    val isLogin = state.mode == AuthMode.Login

    HabiHamScreen {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .imePadding()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = 24.dp, vertical = 32.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                HabiHamBrandTitle("HabiHam")
                IconButton(onClick = onOpenApiSettings) {
                    Icon(Icons.Default.Settings, contentDescription = "Настройки API")
                }
            }
            Spacer(Modifier.height(12.dp))

            HabiHamSegmentTabRow(
                labels = listOf("Вход", "Регистрация"),
                selectedIndex = if (isLogin) 0 else 1,
                onTabSelected = { index ->
                    viewModel.setMode(if (index == 0) AuthMode.Login else AuthMode.Register)
                },
            )

            Spacer(Modifier.height(16.dp))

            SectionTitle(
                text = if (isLogin) "Добро пожаловать" else "Новый аккаунт",
                subtitle = if (isLogin) {
                    "Войдите, чтобы видеть тренировки"
                } else {
                    "Создайте пользователя (роль User, как в вебе)"
                },
                modifier = Modifier.fillMaxWidth(),
            )

            if (!state.successMessage.isNullOrBlank() && isLogin) {
                Spacer(Modifier.height(8.dp))
                Text(
                    state.successMessage!!,
                    style = MaterialTheme.typography.bodySmall,
                    color = HabiHamColors.Success,
                    modifier = Modifier.fillMaxWidth(),
                )
            }

            Spacer(Modifier.height(12.dp))

            HabiHamContentCard {
                OutlinedTextField(
                    value = state.username,
                    onValueChange = viewModel::onUsernameChange,
                    label = { Text("Логин") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    colors = habihamTextFieldColors(),
                    shape = MaterialTheme.shapes.small,
                )
                Spacer(Modifier.height(12.dp))

                OutlinedTextField(
                    value = state.password,
                    onValueChange = viewModel::onPasswordChange,
                    label = { Text("Пароль") },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    visualTransformation = androidx.compose.ui.text.input.PasswordVisualTransformation(),
                    keyboardOptions = KeyboardOptions(
                        keyboardType = androidx.compose.ui.text.input.KeyboardType.Password,
                    ),
                    colors = habihamTextFieldColors(),
                    shape = MaterialTheme.shapes.small,
                )
                if (!isLogin) {
                    Spacer(Modifier.height(8.dp))
                    Text(
                        "Минимум 6 символов",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }

            if (!state.error.isNullOrBlank()) {
                Spacer(Modifier.height(12.dp))
                Text(
                    state.error!!,
                    color = MaterialTheme.colorScheme.error,
                    style = MaterialTheme.typography.bodySmall,
                    modifier = Modifier.fillMaxWidth(),
                )
            }

            Spacer(Modifier.height(20.dp))

            Button(
                onClick = viewModel::submit,
                enabled = !state.isLoading,
                modifier = Modifier.fillMaxWidth(),
                shape = MaterialTheme.shapes.small,
                elevation = ButtonDefaults.buttonElevation(
                    defaultElevation = 4.dp,
                    pressedElevation = 2.dp,
                ),
            ) {
                if (state.isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.height(20.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary,
                    )
                } else {
                    Text(if (isLogin) "Войти" else "Зарегистрироваться")
                }
            }
        }
    }
}
