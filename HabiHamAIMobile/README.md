# HabiHamAIMobile

Android-приложение на **Kotlin** и **Jetpack Compose**: вход по JWT и просмотр **истории силовых тренировок** с фильтрами (как вкладка «История» в веб-клиенте).

## Возможности

- Вход и регистрация: `POST /auth/login`, `POST /auth/register`
- **Сила → Текущая:** программы, старт тренировки, черновик/завершение (`POST /users/me/workouts`)
- **Сила → История:** `GET /users/me/workouts/history`, фильтры по датам и программе
- **Вело:** список `GET /users/me/bike-activities`, импорт TCX, маршрут на карте (OpenStreetMap), удаление
- Сохранение JWT и адреса API между запусками (URL — отдельно от токена, не сбрасывается при выходе)

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

**По умолчанию** (debug и release): **`https://habihamai.onrender.com`**

| Тип сборки | Как задать URL |
|------------|----------------|
| **Любая** | Шестерёнка в приложении → «Адрес API» (сохраняется на устройстве) |
| **Debug** (Run ▶) | `local.properties` → `habiHam.apiBaseUrl=...` — подставляется при сборке |
| **Release** (опционально) | `api-config.properties` → `releaseApiBaseUrl=...` или `-PhabiHam.releaseApiBaseUrl=...` |

#### Локальный API на машине разработчика

1. В **`local.properties`** (не в git):

   ```properties
   habiHam.apiBaseUrl=http://127.0.0.1:5193
   ```

   или Wi‑Fi: `http://192.168.1.241:5193` (свой IPv4 из `ipconfig`).

2. Пересоберите **debug** (Run ▶).

3. Либо без пересборки: шестерёнка → локальный URL (см. ниже про USB / эмулятор).

Если раньше в приложении сохранился старый URL — смените в шестерёнке или очистите данные приложения.

#### Телефон подключён проводом (USB) — без локального DNS

Кабель USB **не передаёт** HTTP на ПК. Локальный DNS тут не поможет: телефон всё равно не «видит» ваш ПК по сети, пока не настроен проброс портов или Wi‑Fi.

**Проще всего — проброс ADB** (телефон и ПК уже связаны для отладки):

```powershell
# Путь к adb (или добавьте platform-tools в PATH)
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" reverse tcp:5193 tcp:5193
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" reverse --list
```

В приложении укажите: **`http://127.0.0.1:5193`** (или `http://localhost:5193`).

API на ПК: `dotnet run --launch-profile http-mobile` (достаточно `localhost:5193` на компьютере).

После отключения USB выполните: `adb reverse --remove tcp:5193`

#### Wi‑Fi вместо провода

1. Телефон и ПК в **одной Wi‑Fi** (мобильный интернет выключите для теста).
2. `ipconfig` → IPv4, например `192.168.1.241`.
3. В приложении: `http://192.168.1.241:5193`
4. В браузере **на телефоне** откройте `http://192.168.1.241:5193/swagger` — если не открывается, чините firewall/API, а не DNS.

#### Локальный DNS (если очень хочется красивое имя)

Имеет смысл только вместе с **Wi‑Fi**, не с одним USB:

- На роутере: запись `habiham.lan` → `192.168.1.241` (если роутер умеет локальный DNS).
- Или [Acrylic DNS](https://mayakron.altervista.org/wikibase/show.php?id=AcrylicHome) на Windows: зона `habiham.lan` → A-запись на IP ПК; в Wi‑Fi на телефоне DNS = IP ПК (сложнее).

Для разработки обычно достаточно **IP** или **adb reverse**, без DNS.

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
- Перед выкладкой в прод: по умолчанию уже `https://habihamai.onrender.com`; при необходимости ограничить `usesCleartextTraffic` только для debug (сейчас разрешён HTTP для локальной разработки).
