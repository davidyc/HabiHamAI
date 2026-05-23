# HabiHamAIMobile

Android-приложение на **Kotlin** и **Jetpack Compose**: вход по JWT и просмотр **истории силовых тренировок** с фильтрами (как вкладка «История» в веб-клиенте).

## Возможности

- Вход: `POST /auth/login`
- **Сила → Текущая:** программы, старт тренировки, черновик/завершение (`POST /users/me/workouts`)
- **Сила → История:** `GET /users/me/workouts/history`, фильтры по датам и программе
- **Вело:** список `GET /users/me/bike-activities`, импорт TCX, маршрут на карте (OpenStreetMap), удаление
- Сохранение JWT и URL API между запусками

## Требования

- [Android Studio](https://developer.android.com/studio) (Ladybug или новее)
- JDK 17 (обычно встроен в Studio)
- Запущенный **HabiHamAIAPI** (`dotnet run --launch-profile http-mobile` в `HabiHamAIAPI/`). `npm run dev` — это веб, не API.

## Запуск

1. Откройте папку `HabiHamAIMobile` в Android Studio (**File → Open**).
2. Дождитесь синхронизации Gradle.
3. Запустите эмулятор или подключите телефон с отладкой по USB.
4. **Run** ▶ `app`.

### URL API

| Среда | URL |
|--------|-----|
| Эмулятор Android | `http://10.0.2.2:5193` (значение по умолчанию) |
| Телефон в той же Wi‑Fi | `http://<IP-вашего-ПК>:5193` |

На экране входа URL можно изменить; он сохраняется вместе с токеном.

Убедитесь, что API слушает не только `localhost` (для телефона), например:

```bash
cd HabiHamAIAPI
dotnet run --launch-profile http-mobile
```

Профиль слушает **все интерфейсы** (`0.0.0.0:5193`). Обычный `dotnet run` (профиль `http`) — только `localhost`, с телефона **не работает**.

## Структура

```
app/src/main/java/com/habiham/mobile/
  data/api/          # Retrofit
  data/model/        # DTO
  data/repository/   # auth, workouts
  ui/login/          # экран входа
  ui/workouts/       # история силовых
  ui/bike/           # велотренировки TCX
  ui/main/           # вкладки Сила / Вело
```

## Дальше

- Запись тренировок, программы, вес — отдельные экраны к тем же эндпоинтам API.
- Release-сборка: только HTTPS, убрать `usesCleartextTraffic` для production.
