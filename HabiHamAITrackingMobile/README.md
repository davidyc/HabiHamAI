# HabiHamAITrackingMobile

Отдельное Android-приложение (**Kotlin**, **Jetpack Compose**) для вкладки **«Трекинг»** веб-клиента: **привычки** и **задачи**.

## Возможности

- Вход / регистрация (`POST /auth/login`, `/auth/register`)
- **Привычки:** обзор, отметки вчера/сегодня, полоска за период; фильтры как в вебе — период (7 / 14 / 30 дн. или свой), категория с счётчиком «выполнено / всего»
- **Задачи:** период (все / месяц / неделя / 3 дня / выходные / свой), статус (все / открытые / готово) со счётчиками, категория, сортировка по названию / категории / дедлайну / статусу
- Категории при создании (из `GET /users/me/categories`)
- Настройка URL API (как в основном мобильном приложении)

## Отличие от HabiHamAIMobile

| HabiHamAIMobile | HabiHamAITrackingMobile |
|-----------------|-------------------------|
| `com.habiham.mobile` | `com.habiham.tracking` |
| Сила + вело | Привычки + задачи |
| Отдельная установка в лаунчере | |

Один и тот же аккаунт на сервере; сессии хранятся **раздельно** (разные package / DataStore).

## Запуск

1. Откройте `HabiHamAITrackingMobile` в Android Studio.
2. Синхронизируйте Gradle, запустите **Run** ▶ `app`.
3. API: `dotnet run --launch-profile http-mobile` в `HabiHamAIAPI/`.

### URL API

- По умолчанию: `https://habihamai.onrender.com`
- Debug: `local.properties` → `habiHam.apiBaseUrl=http://127.0.0.1:5193`
- На устройстве: шестерёнка в приложении

## Структура

```
app/src/main/java/com/habiham/tracking/
  data/api/          # Retrofit TrackingApi
  data/repository/   # TrackingRepository, AuthRepository
  domain/            # логика цикла статусов привычек
  ui/habits/         # вкладка привычек
  ui/todos/          # вкладка задач
  ui/main/           # главный экран с сегментами
```
