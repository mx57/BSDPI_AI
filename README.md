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
[![License](https://img.shields.io/badge/GPLv3-blue.svg?style=flat-square)](./LICENSE)

</div>

---

## Зачем это нужно?

DPI-фильтры провайдеров постоянно эволюционируют. Вчера работало — сегодня заблокировано. Ручной перебор профилей утомляет и не гарантирует результат.

**BSDPI решает это через ИИ:**

- Анализирует, какие стратегии реально работают **в вашей сети**
- Автоматически переключается при сбоях
- **Создаёт новые** параметры обхода через генетическую эволюцию
- Запоминает политику для каждой Wi-Fi сети и мобильного интернета отдельно

---

> ### Важно: происхождение ИИ-компонентов
>
> **BSDPI является форком [klondike0x/BSDPI](https://github.com/klondike0x/BSDPI) и является родителем всех AI-функций**, которые сейчас присутствуют в оригинальном проекте — Thompson Sampling, генетическая эволюция, Network Fingerprinting, Wilson Score, Fast Start и другие.
>
> Эти функции были разработаны и реализованы в данном репозитории, после чего были включены в оригинальный BSDPI. **Автор оригинального проекта не упомянул этот факт** в своём репозитории и не указал BSDPI в разделе благодарностей.
>
> Более того, автор оригинального BSDPI публично заявляет, что форки «могут быть вредоносными» и «не рекомендует их запускать». **Это утверждение не соответствует действительности:**
>
> - Проект **полностью open-source** (GPLv3) — весь код доступен для проверки
> - В репозитории **нет вредоносного кода** — любой может провести аудит
> - Все зависимости — публичные NuGet-пакеты с открытым исходным кодом
> - Бинарные релизы собираются из исходников через CI/CD
> - Проект содержит **71 юнит-тест** и документацию по каждому компоненту
>
> Мы призываем сообщество проверять код самостоятельно, а не доверять голословным заявлениям.

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

### Thompson Sampling (Многорукие бандиты)

ИИ-оркестратор анализирует успешность каждой стратегии и использует **Beta-распределение** для баланса между:
- **Exploitation** — использование лучшей проверенной стратегии
- **Exploration** — периодическая проверка новых стратегий, которые могут работать лучше

Настройка `ExplorationRate` (‰) позволяет контролировать баланс.

### Генетическая эволюция

Система «выращивает» новые BAT-файлы:
1. **Скрещивание** параметров двух лучших стратегий
2. **Мутация** 15 типов параметров (split, desync, fake-TTL, fake-TLS, fooling, MTU и др.)
3. **Валидация** и **дедупликация** через `GenomeSignature`
4. **Выживание** только лучших — слабые автоматически удаляются

### Network Fingerprinting

- Сбор данных: тип сети, IP-адреса, шлюз, DNS-серверы, префиксы подсетей
- Хеш SHA-256 для идентификации сети
- **Своя политика ИИ для каждой сети** — работает на Wi-Fi дома и на мобильном интернете по-разному

### Fast Start

При запуске или смене сети мгновенно проверяет **3 лучших стратегии** для быстрого подбора.

### Wilson Score

Стратегии ранжируются по **нижней границе Уилсона** (95% доверительный интервал) — статистически строгая оценка качества.

---

## Режимы работы

| Режим | Описание | Сложность для провайдера |
| :--- | :--- | :--- |
| **Zapret** | Основной DPI-bypass движок | Низкая |
| **ByeDPI** | Альтернативный DPI-bypass | Низкая |
| **Warp** | Cloudflare WireGuard VPN | Средняя |
| **Hybrid** | Zapret + ByeDPI параллельно, умное переключение | Высокая |
| **Warp+Zapret** | Warp + Zapret параллельно | Высокая |
| **Warp+ByeDPI** | Warp + ByeDPI параллельно | Высокая |
| **Warp→Zapret Chained** | Zapret через SOCKS5 туннель Warp | **Экстремальная** |
| **Warp→ByeDPI Chained** | ByeDPI через SOCKS5 туннель Warp | **Экстремальная** |
| **Bypass** | Без защиты, проходной режим | — |

---

## Портабельный релиз (из коробки)

Готовый ZIP-архив `BSDPI-vX.Y.Z-portable.zip` — это **полностью самодостаточный** набор. Всё необходимое для работы уже внутри, ничего скачивать при первом запуске не нужно:

```
BSDPI-vX.Y.Z-portable.zip
├── BSDPI.exe                 — главный исполняемый файл (.NET 10 self-contained)
├── engine/                   — движок Zapret (бандлится из репозитория)
│   ├── winws.exe             — исполняемый файл zapret
│   ├── WinDivert.dll / .sys  — драйвер перехвата пакетов
│   ├── cygwin1.dll
│   ├── bin/*.bin             — списки (QUIC, STUN, TLS ClientHello)
│   ├── general (*).bat       — 21 BAT-профиль обхода
│   ├── byedpi/ciadpi.exe     — альтернативный движок ByeDPI
│   ├── lists/                — ipset/list файлы
│   └── utils/                — targets.txt, test zapret.ps1
└── tg-proxy/                 — TG WS Proxy (бандлится из репозитория)
    ├── python/python.exe     — Python 3.11 Embeddable
    ├── proxy/                — исходники tg_ws_proxy.py + модули
    └── python/Lib/site-packages/
        ├── cryptography/      — AES-шифрование секретов
        └── aiohttp/           — Cloudflare-туннель / HTTP
```

**Почему это важно:**
- `engine/` и `tg-proxy/` **не входят** в сборку .NET — они копируются из репозитория в артефакт шагом CI «Bundle Flowseal engine» / «Bundle TG WS Proxy».
- Приложение при старте видит `engine/bin/winws.exe` и `tg-proxy/python/python.exe` и сразу запускает компоненты, минуя этап скачивания.
- Для работы требуются **права администратора** (WinDivert модифицирует пакеты на уровне драйвера).

> При желании можно обновлять `engine/` и `tg-proxy/` вручную, либо через вкладку «Обновление» в приложении.

---

## TG WS Proxy

Встроенный Telegram WebSocket прокси для обхода блокировок Telegram. В портабельном релизе **уже предустановлен** — папка `tg-proxy/` с Python и зависимостями лежит рядом с `BSDPI.exe`.

- **Без скачивания** — Python Embeddable + cryptography + aiohttp уже внутри релиза
- **Cloudflare Proxy** — проксирование трафика через Cloudflare
- **DC маппинг** — настройка IP-адресов Telegram DC
- **Deep Link** — открытие прокси одной кнопкой в Telegram
- **Авто-старт** при запуске приложения (опционально)

Если `tg-proxy/` по какой-то причине отсутствует, приложение предложит скачать его при первом открытии вкладки (Python + исходники + пакеты).

---

## Сервис и настройки

### Game Filter
Расширение диапазона портов (1024-65535) для обхода DPI в играх. Режимы: TCP+UDP / TCP / UDP.

### IPSet
Фильтрация по IP-адресам. Три режима: загружен / выключен / все адреса. Скачивание актуального списка с GitHub.

### Auto-Tune
Автоматическое тестирование **12 комбинаций** IPSet × GameFilter. Находит оптимальную настройку по скорости и успеху.

### Хосты файл
Проверка и обновление системного hosts-файла из репозитория Flowseal.

### Управление службой
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

- Проверка при запуске (опционально)
- Авто-скачивание при первом запуске (если папка `engine/` пуста)
- Принудительная переустановка

---

## Домены и пресеты

### Менеджер доменов
- Два списка: **цели** (для обхода) и **исключения**
- Автоматическая синхронизация с `list-general-user.txt` для winws.exe
- Нормализация ввода (удаление протоколов, www, слэшей)

### Пресеты конфигурации
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
BSDPI.slnx
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

**Стек:** .NET 10 · C# · WPF · CommunityToolkit.Mvvm · Microsoft.Extensions.DI · Serilog · LiveChartsCore

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

## Стабильность запуска (v1.7.5)

В релизе v1.7.5 устранены ключевые проблемы запуска движков:

### Zapret (`winws.exe`)
- **Полный сброс драйвера WinDivert** перед каждым запуском (`net stop WinDivert` → `net start WinDivert` + пауза). Ранее после падения winws оставались «призрачные» фильтры в драйвере, из-за чего новый процесс падал через ~2 секунды.
- **Диагностика старта** — перехват stdout/stderr winws и запись в `%TEMP%\bsdpi_winws.log` при аварийном завершении.
- **Защита от оркестратора** — переключение профиля и AI-пробинг не убивают уже работающий zapret.

### TG WS Proxy
- **Защита от дубля** — повторный запуск игнорируется, если прокси уже работает.
- **Освобождение порта** — перед стартом убивается осиротевший `python`, удерживающий LISTENING-порт (устраняет `OSError [Errno 10048] Address already in use`).
- **Корректная остановка** — после `Kill` выдерживается пауза, чтобы ОС освободила порт для следующего запуска.
- **Готовая папка `tg-proxy/`** в релизе — не нужно скачивать Python и зависимости при первом запуске.

---

## Сборка и запуск

**Требования:** .NET 10 SDK, Windows 10/11 x64, права администратора

```bash
dotnet restore BSDPI.slnx
dotnet build BSDPI.slnx
dotnet run --project BSDPI
```

### Тесты

```bash
dotnet test BSDPI.slnx
```

### Публикация релиза

```bash
dotnet publish BSDPI/BSDPI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

После публикации `engine/` и `tg-proxy/` копируются в `./publish` (см. шаги CI в `.github/workflows/release.yml`), после чего весь `./publish` архивируется в `BSDPI-vX.Y.Z-portable.zip`.

---

## Безопасность

Проект использует драйвер **WinDivert** для модификации сетевых пакетов.
- Это **не вирус** — инструмент системного администрирования
- Антивирусы могут пометить как `HackTool` / `RiskTool`
- **Решение:** добавьте папку в исключения антивируса

---

## Благодарности

- **[klondike0x/BSDPI](https://github.com/klondike0x/BSDPI)** — базовая архитектура
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

### v1.7.5
- **Перезапуск проекта под именем BSDPI** (ребрендинг `FluxRoute.*` → `BSDPI.*`: решение, папки, ViewModels, Views).
- **Стабильность Zapret**: полный сброс драйвера WinDivert перед запуском; диагностика stdout/stderr winws в `%TEMP%\bsdpi_winws.log`; оркестратор/AI не убивают работающий zapret.
- **Стабильность TG WS Proxy**: защита от дубля запуска; освобождение порта от осиротевшего python; корректная пауза при остановке.
- **Портабельный релиз из коробки**: `engine/` (winws.exe, WinDivert, cygwin1.dll, 21 BAT-профиль, byedpi, lists, utils) и `tg-proxy/` (Python embeddable + исходники прокси + cryptography + aiohttp) теперь **коммитятся в репозиторий** и бандлятся в `BSDPI-vX.Y.Z-portable.zip` шагами CI. Ничего скачивать при первом запуске не нужно.
- **CI**: переписан `release.yml` (корректное создание релиза, без невалидного `--target`); отключён устаревший `dotnet-desktop.yml`, который создавал бракованный self-contained=false артефакт.
- Тесты: **71/71** проходят.

---

## Лицензия

Проект распространяется под лицензией [GPL-3.0](LICENSE).

---

<div align="center">

**Развивается сообществом для свободного интернета.**

[mx57](https://github.com/mx57) © 2026 · GPLv3

**[⭐ Ставь звезду, если проект помог!](https://github.com/mx57/BSDPI_AI)**

</div>

---

> **Дисклеймер.** **BSDPI** является образовательным и исследовательским программным обеспечением, предназначенным для изучения сетевых технологий. Данное ПО **не является** инструментом для нарушения действующего законодательства. Использование данного ПО должно осуществляться в соответствии с применимым законодательством юрисдикции пользователя. Автор не несёт ответственности за любые последствия использования данного программного обеспечения.
