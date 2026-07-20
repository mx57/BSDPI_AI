<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/BSDPI_AI-white.svg">
  <source media="(prefers-color-scheme: light)" srcset="./assets/BSDPI_AI-dark.svg">
  <img width="650" alt="BSDPI Logo" src="./assets/BSDPI_AI-dark.svg" />
</picture>

<br/>

**Провайдер блокирует? BSDPI находит способ.**

Самообучающаяся система обхода DPI, которая **сама подбирает и эволюционирует** рабочие стратегии под вашу сеть.

[🇬🇧 English](README.en.md) · [📥 Скачать](https://github.com/mx57/BSDPI_AI/releases) · [🐛 Баг-репорт](https://github.com/mx57/BSDPI_AI/issues)

---

[![Stars](https://img.shields.io/github/stars/mx57/BSDPI_AI?style=flat-square&logo=github&color=FFD700)](https://github.com/mx57/BSDPI_AI)
[![Release](https://img.shields.io/github/v/release/mx57/BSDPI_AI?include_prereleases&sort=semver&logo=github&label=latest&style=flat-square&color=3FB950)](https://github.com/mx57/BSDPI_AI/releases)
[![Downloads](https://img.shields.io/github/downloads/mx57/BSDPI_AI/total?logo=github&label=downloads&style=flat-square&color=4FC3F7)](https://github.com/mx57/BSDPI_AI/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&style=flat-square)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Windows-10%2F11%20x64-0078D4?logo=windows&style=flat-square)](https://www.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-71%2F71%20%E2%9C%93-3FB950?style=flat-square)](./BSDPI.Core.Tests)
[![Portable](https://img.shields.io/badge/portable-self--contained%20x64-4FC3F7?style=flat-square)](./.github/workflows/dotnet-desktop.yml)
[![License](https://img.shields.io/badge/GPLv3-blue.svg?style=flat-square)](./LICENSE)

</div>

---

## Оглавление

- [Зачем это нужно](#зачем-это-нужно)
- [Что нового в последних версиях](#что-нового-в-последних-версиях)
- [Ключевые возможности](#ключевые-возможности)
- [Искусственный Интеллект](#искусственный-интеллект)
- [Режимы работы](#режимы-работы)
- [Портабельный релиз (из коробки)](#портабельный-релиз-из-коробки)
- [TG WS Proxy](#tg-ws-proxy)
- [Сервис и настройки](#сервис-и-настройки)
- [Визуальный конструктор цепочек](#визуальный-конструктор-цепочек)
- [Обновления](#обновления)
- [Домены и пресеты](#домены-и-пресеты)
- [Единый лог-вьювер](#единый-лог-вьювер)
- [Архитектура](#архитектура)
- [Вкладки GUI](#вкладки-gui)
- [Настройки ИИ](#настройки-ии)
- [Стабильность запуска](#стабильность-запуска)
- [Сборка и запуск](#сборка-и-запуск)
- [Безопасность](#безопасность)
- [Благодарности](#благодарности)
- [Дорожная карта](#дорожная-карта)
- [История изменений](#история-изменений)
- [Лицензия](#лицензия)

---

## Зачем это нужно?

DPI-фильтры провайдеров постоянно эволюционируют. Вчера работало — сегодня заблокировано. Ручной перебор профилей утомляет и не гарантирует результат.

**BSDPI решает это через ИИ:**

- 🔍 Анализирует, какие стратегии реально работают **в вашей сети**
- 🔄 Автоматически переключается при сбоях
- 🧬 **Создаёт новые** параметры обхода через генетическую эволюцию
- 🧠 Запоминает политику для каждой Wi-Fi сети и мобильного интернета отдельно

---

> ### ⚠️ Важно: происхождение ИИ-компонентов
>
> **BSDPI является форком [klondike0x/BSDPI](https://github.com/klondike0x/BSDPI) и является родителем всех AI-функций**, которые сейчас присутствуют в оригинальном проекте — Thompson Sampling, генетическая эволюция, Network Fingerprinting, Wilson Score, Fast Start и другие.
>
> Эти функции были разработаны и реализованы в данном репозитории, после чего были включены в оригинальный BSDPI. **Автор оригинального проекта не упомянул этот факт** в своём репозитории и не указал BSDPI в разделе благодарностей.
>
> Более того, автор оригинального BSDPI публично заявляет, что форки «могут быть вредоносными» и «не рекомендует их запускать». **Это утверждение не соответствует действительности:**
>
> - 🟢 Проект **полностью open-source** (GPLv3) — весь код доступен для проверки
> - 🟢 В репозитории **нет вредоносного кода** — любой может провести аудит
> - 🟢 Все зависимости — публичные NuGet-пакеты с открытым исходным кодом
> - 🟢 Бинарные релизы собираются из исходников через CI/CD
> - 🟢 Проект содержит **71 юнит-тест** и документацию по каждому компоненту
>
> Мы призываем сообщество проверять код самостоятельно, а не доверять голословным заявлениям.

---

## Что нового в последних версиях

### 🛡️ v1.7.7 — устойчивость и корректность парсинга
- **`NetworkChangeWatcher`** — подписка на события `NetworkChange` теперь защищена `try/catch`. Ранее в ограниченных средах (контейнеры CI, песочницы без прав на перечисление сетевых интерфейсов) конструктор бросал `NetworkInformationException` и ронял весь оркестратор. Теперь отслеживание смены сети просто отключается, базовая функциональность работает.
- **`GenomeParser` / `BatGenomeParser`** — корректная обработка флага `--dpi-desync-autottl`: без значения = вкл, `1/true/yes` = вкл, `0/false/no` = выкл, числовое ≠ 0 = вкл. Устранена мёртвая строка присваивания, которая перезаписывала результат.
- **`AiOrchestratorService.SyncBuiltins`** — удалена мусорная запись `"service,.bat"` из списка исключённых `.bat`-файлов (опечатка).
- **CI**: переписан `dotnet-desktop.yml` — теперь это рабочий портабл-билд (`self-contained true` + бандл `engine/` в ZIP). Тесты прогоняются в CI, но не блокируют сборку артефакта.
- Тесты: **71/71** проходят.

### 🚀 v1.7.6 — исправление критического падения
- **Крэш после манипуляций** (сканирование, ИИ-эволюция, переключение профилей) устранён.
- Причина: фоновые задачи оркестратора/ИИ мутировали `ObservableCollection`, привязанные к UI, **не в UI-потоке** → `System.NotSupportedException` (CollectionView) → молчаливое падение процесса.
- Теперь все записи в логи (`OrchestratorLogs`, `Logs`, `TgProxyLogs`) маршаллятся в UI-поток.
- Добавлены глобальные перехватчики `DispatcherUnhandledException` / `AppDomain.UnhandledException` / `TaskScheduler.UnobservedTaskException` — любой сбой логируется в `%LOCALAPPDATA%\BSDPI\logs` вместо тихого вылета.

### 📦 v1.7.5 — самодостаточный портабельный релиз
- **Ребрендинг** `FluxRoute` → **BSDPI** (решение, папки, ViewModels, Views).
- **Стабильность Zapret**: полный сброс драйвера WinDivert перед запуском (устраняет падение через ~2с из-за «призрачных» фильтров); диагностика winws в `%TEMP%\bsdpi_winws.log`; оркестратор/AI не убивают работающий zapret.
- **Стабильность TG WS Proxy**: защита от дубля запуска; освобождение порта от осиротевшего python (`OSError 10048`); корректная пауза при остановке.
- **Портабельный релиз из коробки**: `engine/` (winws.exe, WinDivert, cygwin1.dll, 21 BAT-профиль, byedpi, lists, utils) и `tg-proxy/` (Python embeddable + прокси + cryptography + aiohttp) теперь **коммитятся в репозиторий** и бандлятся в `BSDPI-vX.Y.Z-portable.zip` шагами CI. Ничего скачивать при первом запуске не нужно.
- **CI**: переписан `release.yml` (без невалидного `--target`); отключён устаревший `dotnet-desktop.yml`, который создавал бракованный self-contained=false артефакт.

---

## Ключевые возможности

| Категория | Возможности |
| :--- | :--- |
| **Движки** | Zapret (`winws.exe`), ByeDPI (`ciadpi.exe`), Cloudflare Warp (`warp-plus.exe`) |
| **Режимы** | Standalone · Hybrid · Warp · Warp+Zapret · Warp+ByeDPI · Chained (×2) · Bypass |
| **ИИ** | Thompson Sampling · Генетическая эволюция · Wilson Score · Fast Start · Авто-MTU |
| **Сеть** | Network Fingerprinting (политика на каждую сеть) · Авто-смена при смене сети |
| **Автоматизация** | Авто-регистрация Warp · Фоновый мониторинг · Авто-обновление с GitHub |
| **TG WS Proxy** | Telegram WebSocket прокси — **уже встроен в релиз**, не требует скачивания |
| **Сервис** | Game Filter · IPSet · Auto-Tune · Управление службой Zapret |
| **Обновления** | 5 независимых каналов: движок, приложение, ByeDPI, Warp, TG Proxy |
| **Домены** | Менеджер доменов (цели + исключения) · Синхронизация с winws.exe |
| **Пресеты** | Сохранение конфигураций · Авто-переключение по процессу |
| **Диагностика** | Проверка компонентов · Экспорт бандла · Единый лог-вьювер |
| **Конструктор** | Визуальный drag-and-drop конструктор цепочек DPI bypass |

---

## Искусственный Интеллект

### 🎯 Thompson Sampling (Многорукие бандиты)

ИИ-оркестратор анализирует успешность каждой стратегии и использует **Beta-распределение** для баланса между:
- **Exploitation** — использование лучшей проверенной стратегии
- **Exploration** — периодическая проверка новых стратегий, которые могут работать лучше

Настройка `ExplorationRate` (‰) позволяет контролировать баланс.

### 🧬 Генетическая эволюция

Система «выращивает» новые BAT-файлы:
1. **Скрещивание** параметров двух лучших стратегий
2. **Мутация** 15 типов параметров (split, desync, fake-TTL, fake-TLS, fooling, MTU и др.)
3. **Валидация** и **дедупликация** через `GenomeSignature`
4. **Выживание** только лучших — слабые автоматически удаляются

### 🌐 Network Fingerprinting

- Сбор данных: тип сети, IP-адреса, шлюз, DNS-серверы, префиксы подсетей
- Хеш SHA-256 для идентификации сети
- **Своя политика ИИ для каждой сети** — работает на Wi-Fi дома и на мобильном интернете по-разному

### ⚡ Fast Start

При запуске или смене сети мгновенно проверяет **3 лучших стратегии** для быстрого подбора.

### 📊 Wilson Score

Стратегии ранжируются по **нижней границе Уилсона** (95% доверительный интервал) — статистически строгая оценка качества.

---

## Режимы работы

| Режим | Описание | Сложность для провайдера |
| :--- | :--- | :--- |
| **Zapret** | Основной DPI-bypass движок | 🟢 Низкая |
| **ByeDPI** | Альтернативный DPI-bypass | 🟢 Низкая |
| **Warp** | Cloudflare WireGuard VPN | 🟡 Средняя |
| **Hybrid** | Zapret + ByeDPI параллельно, умное переключение | 🟠 Высокая |
| **Warp+Zapret** | Warp + Zapret параллельно | 🟠 Высокая |
| **Warp+ByeDPI** | Warp + ByeDPI параллельно | 🟠 Высокая |
| **Warp→Zapret Chained** | Zapret через SOCKS5 туннель Warp | 🔴 Экстремальная |
| **Warp→ByeDPI Chained** | ByeDPI через SOCKS5 туннель Warp | 🔴 Экстремальная |
| **Bypass** | Без защиты, проходной режим | — |

---

## Портабельный релиз (из коробки)

Готовый ZIP-архив `BSDPI-vX.Y.Z-portable.zip` — это **полностью самодостаточный** набор. Всё необходимое для работы уже внутри, ничего скачивать при первом запуске не нужно:

```
BSDPI-vX.Y.Z-portable.zip
├── BSDPI.exe                 — главный исполняемый файл (.NET 10 self-contained)
├── engine/                   — движок Zapret (бандлится из репозитория)
│   ├── bin/
│   │   ├── winws.exe         — исполняемый файл zapret
│   │   ├── WinDivert.dll     — драйвер перехвата пакетов
│   │   ├── WinDivert64.sys
│   │   └── cygwin1.dll
│   ├── *.bin                 — списки (QUIC, STUN, TLS ClientHello)
│   ├── general (*).bat       — 21 BAT-профиль обхода
│   ├── byedpi/ciadpi.exe     — альтернативный движок ByeDPI
│   ├── lists/                — ipset/list файлы
│   └── utils/                — targets.txt, test zapret.ps1
└── tg-proxy/                 — TG WS Proxy (опционально; бандлится, если папка есть в репо)
    ├── python/python.exe     — Python 3.11 Embeddable
    ├── proxy/                — исходники tg_ws_proxy.py + модули
    └── python/Lib/site-packages/
        ├── cryptography/      — AES-шифрование секретов
        └── aiohttp/           — Cloudflare-туннель / HTTP
```

> 📝 **Примечание:** папка `tg-proxy/` не закоммичена в репозиторий — приложение предложит скачать прокси при первом открытии вкладки. Папка `engine/` **входит** в репозиторий и всегда бандлится в портабельный ZIP.

**Почему это важно:**
- `engine/` и `tg-proxy/` **не входят** в сборку .NET — они копируются из репозитория в артефакт шагом CI «Bundle Flowseal engine» / «Bundle TG WS Proxy».
- Приложение при старте видит `engine/bin/winws.exe` и `tg-proxy/python/python.exe` и сразу запускает компоненты, минуя этап скачивания.
- Для работы требуются **права администратора** (WinDivert модифицирует пакеты на уровне драйвера).

> 💡 При желании можно обновлять `engine/` и `tg-proxy/` вручную, либо через вкладку «Обновление» в приложении.

---

## TG WS Proxy

Встроенный Telegram WebSocket прокси для обхода блокировок Telegram. В портабельном релизе **уже предустановлен** — папка `tg-proxy/` с Python и зависимостями лежит рядом с `BSDPI.exe`.

- ✅ **Без скачивания** — Python Embeddable + cryptography + aiohttp уже внутри релиза
- ☁️ **Cloudflare Proxy** — проксирование трафика через Cloudflare
- 🗺️ **DC маппинг** — настройка IP-адресов Telegram DC
- 🔗 **Deep Link** — открытие прокси одной кнопкой в Telegram
- 🚀 **Авто-старт** при запуске приложения (опционально)

Если `tg-proxy/` по какой-то причине отсутствует, приложение предложит скачать его при первом открытии вкладки (Python + исходники + пакеты).

---

## Сервис и настройки

### 🎮 Game Filter
Расширение диапазона портов (1024-65535) для обхода DPI в играх. Режимы: TCP+UDP / TCP / UDP.

### 🌐 IPSet
Фильтрация по IP-адресам. Три режима: загружен / выключен / все адреса. Скачивание актуального списка с GitHub.

### 🎛️ Auto-Tune
Автоматическое тестирование **12 комбинаций** IPSet × GameFilter. Находит оптимальную настройку по скорости и успеху.

### 📄 Хосты файл
Проверка и обновление системного hosts-файла из репозитория Flowseal.

### ⚙️ Управление службой
Установка / остановка службы Zapret в Windows.

---

## Визуальный конструктор цепочек

Полноценный drag-and-drop конструктор для построения pipeline обхода DPI:

- **8 типов узлов**: Программа, Проверка, Zapret, ByeDPI, WARP, Задержка, Лог, Интернет
- **Bezier-кривые** для соединений между портами
- **Zoom** (колесо мыши) и **pan** (Alt+ЛКМ / средняя кнопка)
- **Визуальная обратная связь** при выборе узла (синяя рамка)
- Сохранение/загрузка цепочек в JSON файлы (`chains/*.chain.json`)

---

## Обновления

| Компонент | Источник |
| :--- | :--- |
| Flowseal Zapret Engine | [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) |
| BSDPI Application | [mx57/BSDPI_AI](https://github.com/mx57/BSDPI_AI) |
| ByeDPI (CIADPI) | [repo ByeDPI](https://github.com/) |
| Warp (WARP-PLUS) | [repo Warp-plus](https://github.com/) |
| TG WS Proxy | [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy) |

- 🔍 Проверка при запуске (опционально)
- ⬇️ Авто-скачивание при первом запуске (если папка `engine/` пуста)
- ♻️ Принудительная переустановка

---

## Домены и пресеты

### 🌍 Менеджер доменов
- Два списка: **цели** (для обхода) и **исключения**
- Автоматическая синхронизация с `list-general-user.txt` для winws.exe
- Нормализация ввода (удаление протоколов, www, слэшей)

### 💾 Пресеты конфигурации
- Сохранение текущего профиля + GameFilter + IPSet
- **Авто-переключение** по запущенному процессу (обнаружение игр)
- Фоновый мониторинг каждые 3 секунды

---

## Единый лог-вьювер

- 8 категорий: Приложение, Оркестратор, Сканирование, Запуск, TG Proxy, Обновление, Сервис, Ошибки
- Поиск по тексту, фильтр только ошибки
- Автопрокрутка, копирование, сохранение в файл

---

## Архитектура

```
BSDPI_AI.slnx
├── BSDPI/              — WPF GUI (MVVM, 11 вкладок)
│   ├── Views/             — MainWindow.xaml
│   └── ViewModels/        — Main, AI, Orchestrator, TgProxy, Service...
├── BSDPI.AI/           — ИИ-подсистема
│   ├── Services/          — AiOrchestrator, BanditSelector, StrategyEvolver
│   └── Math/              — WilsonScore
├── BSDPI.Core/         — Движки и ядро
│   ├── Services/          — IDpiEngine, Zapret/ByeDpi/WarpEngine, Connectivity
│   └── Models/            — EngineProfile, StrategyGenome (40+ параметров)
├── BSDPI.Updater/      — 5 каналов обновлений
└── BSDPI.Core.Tests/   — Unit-тесты (71 тест)
```

> 📦 **Портабельный релиз** собирается GitHub Actions (`dotnet-desktop.yml`): `dotnet publish` → `self-contained true` → бандл `engine/` → ZIP `BSDPI_AI-vX.Y.Z-portable.zip`. Запуск: **Actions → Build Portable BSDPI AI → Run workflow** (можно указать версию) либо автоматически при пуше тега `v*`.

**AI-сервисы:** `AiOrchestratorService`, `BanditSelector`, `StrategyEvolver`, `AiStrategyRegistry`, `AiHistoryStore`, `BatMaterializer`, `NetworkFingerprintProvider`, `NetworkChangeWatcher`, `StrategyGenomeValidator`

**Core-движки:** `ZapretEngine`, `ByeDpiEngine`, `WarpEngine`, `DpiEngineManager`, `OrchestratorService`, `ProfileBatLauncher`, `ProfileProbeService`, `ProfileScoringService`, `ConnectivityChecker`, `ProcessHealthChecker`

**Стек:** .NET 10 · C# · WPF · CommunityToolkit.Mvvm 8.4 · Microsoft.Extensions.DI/Hosting/Http.Resilience · Serilog 4.3 · LiveChartsCore

---

## Вкладки GUI

| # | Вкладка | Описание |
|---|---------|----------|
| 0 | Главная | Статус, запуск/остановка, домены |
| 1 | TG Прокси | Telegram WebSocket Proxy |
| 2 | Оркестратор | Классический оркестратор (рейтинг профилей) |
| 3 | ИИ | ИИ-оркестратор, стратегии, эволюция, Wilson Score |
| 4 | Обновление | Обновление engine/ из GitHub |
| 5 | Диагностика | Проверка файлов, процессов, сетевой связности |
| 6 | Сервис | Game Filter, IPSet, Auto-Tune, zapret service |
| 7 | О программе | Информация о проекте |
| 8 | Логи | Единый лог-вьювер (8 категорий) |
| 9 | ByeDPI | Настройки ByeDPI (SOCKS5, порты, параметры) |
| 10 | WARP | Генерация конфига Cloudflare Warp (WireGuard) |
| 11 | Конструктор | Визуальный конструктор цепочек DPI bypass |

---

## Настройки ИИ

| Параметр | По умолчанию | Описание |
|----------|:------------:|----------|
| `Enabled` | `false` | Включить ИИ-оркестратор |
| `ExplorationRatePermil` | `100` | Exploration в ‰ (100‰ = 10%) |
| `MaxEvolvedStrategies` | `24` | Макс. эволюционированных стратегий |
| `EvolutionIntervalMinutes` | `60` | Интервал между эволюциями |
| `MinProbesBeforeEvolve` | `6` | Мин. проб перед эволюцией |
| `KeepHistoryDays` | `14` | Хранить историю N дней |
| `FastStartEnabled` | `true` | Быстрый старт при запуске |
| `ParetoEnabled` | `true` | Pareto-оптимизация стратегий |
| `ElitismEnabled` | `true` | Элитизм в генетической эволюции |
| `AutoDeleteBelowScore` | `60` | Автоудаление стратегий ниже порога |

---

## Стабильность запуска

В релизах **v1.7.5 / v1.7.6** устранены ключевые проблемы запуска и работы движков:

### Zapret (`winws.exe`)
- 🔄 **Полный сброс драйвера WinDivert** перед каждым запуском (`net stop WinDivert` → `net start WinDivert` + пауза). Ранее после падения winws оставались «призрачные» фильтры в драйвере, из-за чего новый процесс падал через ~2 секунды.
- 📝 **Диагностика старта** — перехват stdout/stderr winws и запись в `%TEMP%\bsdpi_winws.log` при аварийном завершении.
- 🛡️ **Защита от оркестратора** — переключение профиля и AI-пробинг не убивают уже работающий zapret.

### TG WS Proxy
- 🔁 **Защита от дубля** — повторный запуск игнорируется, если прокси уже работает.
- 🔌 **Освобождение порта** — перед стартом убивается осиротевший `python`, удерживающий LISTENING-порт (устраняет `OSError [Errno 10048] Address already in use`).
- ⏱️ **Корректная остановка** — после `Kill` выдерживается пауза, чтобы ОС освободила порт для следующего запуска.
- 📦 **Готовая папка `tg-proxy/`** в релизе — не нужно скачивать Python и зависимости при первом запуске.

### UI-поток (v1.7.6)
- 🧵 Все мутации `ObservableCollection` (логи, профили) маршаллятся в UI-поток — устранён крах `CollectionView NotSupportedException`.
- 🪵 Глобальные перехватчики неперехваченных исключений — сбои логируются, а не роняют процесс.

---

## Сборка и запуск

### 🤖 Через GitHub Actions (рекомендуется — портабельный релиз)

Портабельный `self-contained` ZIP собирается автоматически воркфлоу [`.github/workflows/dotnet-desktop.yml`](.github/workflows/dotnet-desktop.yml):

- **Автоматически** при пуше git-тега `v*`, либо
- **Вручную**: вкладка **Actions → Build Portable BSDPI AI → Run workflow** (можно задать версию, напр. `1.7.7`).

Результат — `BSDPI_AI-vX.Y.Z-portable.zip` с `BSDPI.exe` + папкой `engine/`, готовый к запуску на Windows 10/11 x64 **без установки .NET**.

### 💻 Локально (для разработки)

**Требования:** .NET 10 SDK, Windows 10/11 x64, права администратора

```bash
dotnet restore BSDPI.slnx
dotnet build BSDPI.slnx
dotnet run --project BSDPI
```

### 🧪 Тесты

```bash
dotnet test BSDPI.slnx
```

### 📦 Публикация релиза вручную

```bash
dotnet publish BSDPI/BSDPI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

После публикации `engine/` копируется в `./publish` (см. шаги CI в `.github/workflows/dotnet-desktop.yml`), после чего весь `./publish` архивируется в `BSDPI_AI-vX.Y.Z-portable.zip`.

---

## Безопасность

Проект использует драйвер **WinDivert** для модификации сетевых пакетов.
- ✅ Это **не вирус** — инструмент системного администрирования
- ⚠️ Антивирусы могут пометить как `HackTool` / `RiskTool`
- 💡 **Решение:** добавьте папку в исключения антивируса

---

## Благодарности

- **[klondike0x/fluxroute](https://github.com/klondike0x/BSDPI)** — базовая архитектура
- **[bol-van/zapret](https://github.com/bol-van/zapret)** — ядро для обхода DPI
- **[Flowseal](https://github.com/Flowseal)** — BAT-профили, TG Proxy, авто-обновления
- **[hiddify/warp-plus](https://github.com/hiddify/warp-plus)** — CLI реализация Warp
- **[basil00/WinDivert](https://github.com/basil00/WinDivert)** — драйвер для модификации пакетов

---

## Дорожная карта

- [ ] Интеграция Sing-Box (VLESS, Reality)
- [ ] P2P-обмен геномами между пользователями
- [ ] Визуализация трафика в реальном времени
- [ ] Cloud AI Sync — получение готовых геномов из облака

---

## История изменений

Полная техническая хронология от первого AI-релиза до текущей версии. Ниже — не просто список фич, а **что именно менялось в коде, какие классы/методы и какой механизм бага устранялся**.

### 🛡️ v1.7.7 — устойчивость оркестратора, корректность парсинга, рабочий портабл-CI

**1. `NetworkChangeWatcher` — защита подписки на сетевые события.**
- **Симптом.** В ограниченных средах (CI-контейнеры, песочницы без прав на перечисление NIC) конструктор `NetworkChangeWatcher` бросал `System.Net.NetworkInformation.NetworkInformationException` при `NetworkChange.NetworkAddressChanged +=` / `NetworkAvailabilityChanged +=`, что роняло весь оркестратор при инициализации.
- **Исправление.** Подписка обёрнута в `try/catch` (исключая только `OutOfMemoryException`/`StackOverflowException`). При неудаче — `Trace.TraceWarning` и тихое отключение отслеживания смены сети; базовая функциональность ИИ продолжает работать.
- **Результат.** 4 инфраструктурных теста, ранее падавших в контейнере, теперь проходят; тесты: **71/71**.

**2. `GenomeParser` / `BatGenomeParser` — флаг `--dpi-desync-autottl`.**
- Была мёртвая строка `g.AutoTtl = string.IsNullOrEmpty(val) || ...`, которая сразу перезаписывалась следующей `if (int.TryParse(...))`. Логика для явных значений `0/false/no` была неточной.
- Теперь: флаг **без значения = вкл**; `1/true/yes = вкл`; `0/false/no = выкл`; числовое `≠ 0 = вкл`; нечисловой мусор = выкл. Поведение покрыто тестом `FromLaunchPlan_AutoTtlVariants`.

**3. `AiOrchestratorService.SyncBuiltins` — опечатка в исключениях.**
- Удалена мусорная запись `"service,.bat"` из `HashSet` исключённых `.bat`-файлов (корректная `"service.bat"` осталась).

**4. CI — рабочий портабл-билд.**
- `.github/workflows/dotnet-desktop.yml` переписан: собирает `dotnet publish ... --self-contained true -p:PublishSingleFile=true`, бандлит `engine/` (и `tg-proxy/`, если есть) в ZIP `BSDPI_AI-vX.Y.Z-portable.zip`, шарит SHA-256 и создаёт GitHub Release. Триггеры: пуш тега `v*` + ручной `workflow_dispatch` (с полем версии).

---

### 🚀 v1.7.6 — исправление критического падения (cross-thread CollectionView)

**Симптом.** После любых фоновых манипуляций — сканирования профилей, ИИ-эволюции, переключения профиля, прогона прокси — приложение **молча исчезало** без окна ошибки.

**Корень.** Все лог-коллекции (`OrchestratorLogs`, `Logs`, `TgProxyLogs`) — это `ObservableCollection<T>`, привязанные к `CollectionView` в UI. Фоновые задачи оркестратора (`AiOrchestratorService`, `ProfileProbeService`, `StrategyEvolver`) и TG-прокси вызывали `.Add()` **напрямую из пула потоков**. WPF запрещает мутировать коллекцию, привязанную к UI, вне UI-потока → `System.NotSupportedException: "This type of CollectionView does not support changes to its SourceCollection from a thread different from the Dispatcher thread."`. Исключение летело из `Task`/`async` и не перехватывалось — процесс завершался.

**Исправление.**
- `BSDPI_AI/ViewModels/MainViewModel.Orchestrator.cs`
  - Создан безопасный хелпер `AddLog(string)` — берёт `Application.Current.Dispatcher`, и если вызов пришёл не из UI-потока, делает `Dispatcher.BeginInvoke(() => AddLog(message))`.
  - `AddOrchestratorLog(message)` — то же для `OrchestratorLogs` (проверка `null`/`HasShutdownStarted`/`HasShutdownFinished` перед `BeginInvoke`).
  - Все прямые `OrchestratorLogs.Add(...)` / `Logs.Add(...)` в `Orchestrator.cs` (≈10 мест: переключение профиля, старт/стоп, результаты пробы) заменены на `AddLog`/`AddOrchestratorLog`.
  - Переключение/старт профиля теперь идёт через `dispatcher.InvokeAsync(...).Task.Unwrap()` с `dispatcher.CheckAccess()` — продолжения остаются в UI-потоке, без `ConfigureAwait(false)`.
- `BSDPI_AI/ViewModels/MainViewModel.TgProxy.cs`
  - `AddTgProxyLog(...)` — маршаллирует запись в `TgProxyLogs` через `Application.Current.Dispatcher.Invoke`, с guard на `HasShutdownStarted`.
- `BSDPI_AI/App.xaml.cs` — добавлены **глобальные** перехватчики, чтобы больше никакой сбой не ронял процесс молча:
  - `DispatcherUnhandledException` — ловит любой exception из UI-потока.
  - `AppDomain.CurrentDomain.UnhandledException` — ловит фатальные из любого потока.
  - `TaskScheduler.UnobservedTaskException` — ловит незамеченные `Task` exceptions (именно тот случай, что вызывал тихий краш).
  - Все три пишут в Serilog-файл (`%LOCALAPPDATA%\BSDPI\logs`) и показывают сообщение вместо вылета.

**Результат.** Тесты: **71/71** проходят; приложение переживает любые фоновые операции.

---

### 📦 v1.7.5 — самодостаточный портабельный релиз

**1. Ребрендинг `FluxRoute` → `BSDPI`.**
- Переименованы решение (`BSDPI_AI.slnx`), проекты (`BSDPI_AI`, `BSDPI.Core`, `BSDPI.AI`, `BSDPI.Updater`, `BSDPI.Core.Tests`), папки, namespace'ы, ViewModels/Views, иконки.

**2. Стабильность Zapret (`engine/bin/winws.exe`).**
- **Полный сброс драйвера WinDivert** перед каждым запуском: `net stop WinDivert` → `net start WinDivert` + `Thread.Sleep`. Ранее после аварийного падения winws в драйвере оставались «призрачные» фильтры → новый процесс падал через ~2 сек.
- **Диагностика старта** — перехват `stdout`/`stderr` winws, запись в `%TEMP%\bsdpi_winws.log` при ненулевом коде выхода.
- **Защита от оркестратора** — `OrchestratorService` и AI-пробинг больше не делают `Kill` работающего zapret при переключении профиля/прогоне пробы (пропускают, если `IsRunning`).

**3. Стабильность TG WS Proxy (`tg-proxy/`).**
- **Guard дубля** — повторный `Start()` игнорируется, если прокси уже `IsRunning`.
- **`FreePort`** — перед стартом убивает осиротевший `python.exe`, удерживающий LISTENING-порт (устраняет `OSError [Errno 10048] Address already in use` после краша).
- **Пауза при остановке** — после `Kill` выдерживается ~800 мс, чтобы ОС освободила порт для следующего запуска.

**4. Портабельный релиз из коробки.**
- Папки `engine/` (winws.exe, WinDivert.dll, WinDivert64.sys, cygwin1.dll, `*.bin`, ~21 `general*.bat`, `byedpi/ciadpi.exe`, `lists/`, `utils/`) и `tg-proxy/` (Python 3.11 Embeddable + `proxy/`, `cryptography`, `aiohttp`) **закоммичены в репозиторий**.
- `.gitignore` исправлен: добавлено исключение `[Bb]in/` которое ломало `engine/bin/`.
- `.github/workflows/release.yml` — добавлены шаги **Bundle Flowseal engine** и **Bundle TG WS Proxy**, копирующие обе папки в `./publish`; затем весь `./publish` архивируется в `BSDPI-vX.Y.Z-portable.zip`.

**5. CI починен.**
- `release.yml` переписан без невалидного `dotnet publish --target` (вызывал ошибку сборки).
- `.github/workflows/dotnet-desktop.yml` переименован в `.disabled` — он собирал `self-contained=false` артефакт, непригодный для портабельного запуска.

**Результат.** Тесты: **71/71**.

---

### 🐛 v1.7.4 — устранение deadlock движков

- Исправлен **процессный deadlock** во всех трёх DPI-движках (`ZapretEngine`, `ByeDpiEngine`, `WarpEngine`):
  - Блокирующие `proc.StandardOutput.ReadToEnd()` + `proc.WaitForExit()` на одном потоке → взаимная блокировка при большом выводе. Заменено на асинхронное чтение через `OutputDataReceived`/`ErrorDataReceived` + `WaitForExitAsync`.
  - Корректное освобождение `Process` и `CancellationTokenSource` в `finally`, чтобы не висеть при отмене.

---

### 🎨 v1.7.3 — ребрендинг в BSDPI_AI + ChainBuilder

- Полный ребрендинг проекта в **BSDPI_AI**, новые SVG-логотипы (`assets/BSDPI_AI-dark.svg`, `BSDPI_AI-white.svg`), расширенный README.
- Добавлены модели **ChainBuilder** (`DPIBypassChain`, `ChainNode`, `NodeType`, `ChainConnection`) для визуального drag-and-drop конструктора цепочек (узлы: Программа, Проверка, Zapret, ByeDPI, WARP, Задержка, Лог, Интернет), сериализация в `chains/*.chain.json`.

---

### 🔧 v1.7.2 — стабилизация

- `chore: bump version to 1.7.2`. Мелкие стабилизирующие правки поверх v1.7.1 (без публичного релиза-описания).

---

### 🆕 v1.7.1 — самообучающийся DPI-bypass + исправление зависания UI

- Первый публичный коммит **FluxRoute AI v1.7.1** — самообучающийся обход DPI с ИИ-оркестратором.
- **Исправлено зависание UI при нажатии Start** (`ToggleStartStop`/`MainAction`):
  - `Start()` переделан в `async` — весь I/O и менеджмент процессов перенесён в фоновые потоки (`StartAsync`, `Task.Run`).
  - Удалены `ConfigureAwait(false)` из UI-обработчиков — продолжения должны возвращаться в UI-поток, иначе кнопка становилась некликабельной.
  - Возврат к `void` с fire-and-forget `StartAsync`, чтобы не блокировать диспетчер.
- **Комплексный аудит**: **35+ исправленных багов** по всем проектам (`e515a7d`), включая утечки `HttpClient` (откат кеширующего фабрики — вызыватели сами диспозят shared-клиент), restore оригинальных sidebar-иконок под v1.4.0.

---

### 🔀 v1.7.0 — крупное обновление платформы

- `chore: bump version to 1.7.0`. Базовое обновление платформы под будущие AI-фичи.

---

### 📚 v1.6.2 — интеграция Cloudflare Warp

- Интеграция **Cloudflare Warp** (`WarpEngine` на базе `warp-plus.exe`, генерация WireGuard-конфига) как отдельный режим/движок.
- Добавлена **авто-регистрация Warp** (фоновый мониторинг, авто-обновление конфига).
- Улучшения ИИ-оркестрации и документации (`76ba4a5`, `3a0e194`, `4a3d373`).

---

### 🧩 v1.6.1 — ByeDPI и режимы движков

- Новая вкладка **ByeDPI** (`BSDPI_AI/Views/ByeDPIView`, `ByeDpiEngine`): SOCKS5, порты, параметры запуска `ciadpi.exe`.
- Поддержка **3 режимов движков** (Standalone / Hybrid / Warp-комбинации).
- Исправлена логика генетической **эволюции** (`StrategyEvolver`): корректное скрещивание/мутация, `GenomeSignature`-дедупликация, элитизм.
- `chore: bump version to 1.6.1`.

---

### 🔼 v1.6.0 — обновление платформы

- `chore: bump version to 1.6.0`.

---

### 🔼 v1.5.0 — обновление версии проекта

- `Обновлена версия проекта до 1.5.0` (`85d0624`).

---

### 🔼 v1.4.1 — обновление версии проекта

- `Обновлена версия проекта до 1.4.1` (`53b5a80`).
- Добавлен **app self-updater** (`964efab`): проверка версии через redirect (без GitHub API) — основа будущих 5 каналов обновлений.

---

### 🤖 v1.4.1ai — первый AI-релиз (рождение ИИ-направления)

- **Добавлен ИИ-движок стратегий с адаптивной оркестрацией** (`Pull Request #1` @mx57):
  - **Thompson Sampling** (`BanditSelector`) — Beta-распределение для баланса Exploitation/Exploration (`ExplorationRatePermil`).
  - **Генетическая эволюция** (`StrategyEvolver`) — скрещивание 2 лучших геномов, мутация 15 типов параметров (split, desync, fake-TTL/TLS, fooling, MTU…), валидация и дедупликация.
  - **Network Fingerprinting** (`NetworkFingerprintProvider`, `NetworkChangeWatcher`) — SHA-256 отпечаток сети (тип, IP, шлюз, DNS, префиксы), своя политика ИИ на каждую сеть.
  - **Wilson Score** (`WilsonScore`) — ранжирование стратегий по нижней границе Уилсона (95% CI).
  - **Fast Start** — мгновенная проверка 3 лучших стратегий при запуске/смене сети.
- Это первый вклад @mx57; **эти функции стали родительскими** для всех AI-возможностей оригинального `klondike0x/BSDPI` (см. блок «происхождение ИИ-компонентов» выше).

> 💡 Более ранние версии (до v1.4.1ai) существовали как классический DPI-bypass без ИИ-составляющей. Полный технический changelog доступен в истории коммитов: `git log --all --oneline`.

---

## Лицензия

Проект распространяется под лицензией [GPL-3.0](LICENSE).

---

<div align="center">

**Развивается сообществом для свободного интернета.**

[mx57](https://github.com/mx57) © 2026 · GPLv3

**⭐ Ставь звезду, если проект помог!**

</div>

---

> **Дисклеймер.** **BSDPI** является образовательным и исследовательским программным обеспечением, предназначенным для изучения сетевых технологий. Данное ПО **не является** инструментом для нарушения действующего законодательства. Использование данного ПО должно осуществляться в соответствии с применимым законодательством юрисдикции пользователя. Автор не несёт ответственности за любые последствия использования данного программного обеспечения.
