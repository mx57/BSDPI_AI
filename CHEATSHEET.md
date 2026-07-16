# BSDPI AI — Шпаргалка

## Статус улучшений (обновлено: 2026-06-17)
- ✅ Приоритет 1 (Надёжность): 5/5 задач выполнено
- 🔄 Приоритет 2 (ИИ): 2/8 задач выполнено
- ✅ Приоритет 3 (Производительность): 2/4 задачи выполнено
- ✅ Приоритет 4 (UX): 2/6 задач выполнено (конструктор + график)
- 🔄 Приоритет 5 (Тестирование): 4/7 задач выполнено
- 🔄 Приоритет 6 (Архитектура): 1/4 задачи выполнено

**Общий прогресс:** 16/34 задач (47.1%)
**Тесты:** 53/53 зелёные

**Подробная документация:**
- [IMPROVEMENTS_SUMMARY.md](IMPROVEMENTS_SUMMARY.md) — Документация улучшений
- [SESSION_SUMMARY.md](SESSION_SUMMARY.md) — Итоги сессии

## Архитектура проекта

```
BSDPI.slnx
├── BSDPI.AI/          — ИИ-подсистема (Thompson Sampling + генетическая эволюция)
├── BSDPI.Core/        — Ядро: движки DPI, проверка связности, оркестратор, ChainBuilder
├── BSDPI/             — WPF GUI (MVVM: ViewModels + Views + Controls)
├── BSDPI.Updater/     — Автообновление engine/
└── BSDPI.Core.Tests/  — Юнит-тесты (53 теста)
```

## Ключевые компоненты ИИ (BSDPI.AI)

| Файл | Назначение |
|------|------------|
| `Services/AiOrchestratorService.cs` | Главный цикл ИИ: проверка → оценка → смена/эволюция |
| `Services/BanditSelector.cs` | Thompson Sampling: выбор стратегии по Alpha/Beta распределению |
| `Services/StrategyEvolver.cs` | Генетическая эволюция: скрещивание (crossover) + мутации |
| `Services/AiStrategyRegistry.cs` | Реестр геномов + bandit-состояние (JSON-файл) |
| `Services/AiHistoryStore.cs` | История проб (JSONL-файл) |
| `Services/BatMaterializer.cs` | Генерация .bat файлов из геномов |
| `Services/GenomeParser.cs` | Парсинг .bat → StrategyGenome |
| `Services/BatGenomeParser.cs` | Альтернативный парсинг BAT |
| `Services/NetworkFingerprintProvider.cs` | Отпечаток сети (DNS, шлюз, интерфейсы) |
| `Services/NetworkChangeWatcher.cs` | Отслеживание смены сети |
| `Services/StrategyGenomeValidator.cs` | Валидация геномов |
| `Models/StrategyGenome.cs` | Типизированное представление стратегии |
| `Models/NetworkFingerprint.cs` | Отпечаток сети |
| `Models/ProbeOutcome.cs` | Результат пробы |
| `Models/StrategyOrigin.cs` | Builtin / Evolved / Manual |
| `Models/GenomeSignature.cs` | SHA256-подпись генома |
| `Math/WilsonScore.cs` | Нижняя граница Wilson для ранжирования |

## Ключевые компоненты ядра (BSDPI.Core)

| Файл | Назначение |
|------|------------|
| `Services/OrchestratorService.cs` | Классический оркестратор (простой рейтинг) |
| `Services/ConnectivityChecker.cs` | HTTP/Ping/SOCKS5 проверка сайтов |
| `Services/DpiEngineManager.cs` | Менеджер движков (zapret + byedpi), гибридный режим |
| `Services/ZapretEngine.cs` | Движок zapret (winws.exe) |
| `Services/ByeDpiEngine.cs` | Движок ByeDPI (ciadpi.exe) |
| `Services/ProfileProbeService.cs` | Профилирование: запуск → проверка → оценка |
| `Services/ProfileBatLauncher.cs` | Запуск .bat файлов |
| `Services/ProcessHealthChecker.cs` | Проверка здоровья процессов |
| `Services/SettingsService.cs` | Сохранение/загрузка настроек |
| `Services/ChainBuilder/ChainStore.cs` | Хранение цепочек DPI bypass (JSON) |
| `Models/AiSettings.cs` | Настройки ИИ (exploration, эволюция, автоудаление) |
| `Models/EngineProfile.cs` | Профиль движка (параметры запуска) |
| `Models/ChainBuilder/` | Модели цепочек: ChainDefinition, ChainNode, ChainConnection, ChainNodeType |

## WPF GUI (BSDPI/)

| Файл | Назначение |
|------|------------|
| `ViewModels/MainViewModel.cs` | Главная ViewModel (11 вкладок) |
| `ViewModels/MainViewModel.Ai.cs` | ИИ-вкладка: стратегии, эволюция, bandit |
| `ViewModels/MainViewModel.Orchestrator.cs` | Оркестратор |
| `ViewModels/MainViewModel.Process.cs` | Процессы (запуск/остановка winws) |
| `ViewModels/MainViewModel.Service.cs` | Сервис (game filter, ipset) |
| `ViewModels/MainViewModel.TgProxy.cs` | Telegram WS Proxy |
| `ViewModels/MainViewModel.Updates.cs` | Обновления |
| `ViewModels/MainViewModel.Diagnostics.cs` | Диагностика |
| `ViewModels/MainViewModel.Logs.cs` | Логи |
| `ViewModels/ChainBuilderViewModel.cs` | Конструктор цепочек: CRUD, canvas sync |
| `Controls/NodeCanvas.cs` | Canvas с drag-and-drop, zoom, pan |
| `Controls/NodeControl.cs` | UserControl для нод на canvas |
| `Controls/ConnectionLine.cs` | Bezier-линии для соединений |
| `Controls/NodeAppearance.cs` | Цвета и иконки для типов нод |
| `Converters/NullToVisibilityConverter.cs` | Конвертер null → Collapsed |
| `Views/MainWindow.xaml` | Главное окно (11 вкладок) |

## Вкладки GUI

| # | Вкладка | Описание |
|---|---------|----------|
| 0 | ГЛАВНАЯ | Статус, запуск/остановка, домены |
| 1 | TG ПРОКСИ | Telegram WebSocket Proxy |
| 2 | ОРКЕСТРАТОР | Классический оркестратор (рейтинг) |
| 3 | ИИ | ИИ-оркестратор, стратегии, эволюция |
| 4 | ОБНОВЛЕНИЕ | Обновление engine/ |
| 5 | ДИАГНОСТИКА | Проверка файлов, процессов |
| 6 | СЕРВИС | Game filter, ipset, zapret service |
| 7 | О ПРОГРАММЕ | Информация |
| 8 | ЛОГИ | Логи приложения |
| 9 | BYEDPI | Настройки ByeDPI |
| 10 | WARP | Генерация конфига Cloudflare Warp |
| 11 | КОНСТРУКТОР | Визуальный конструктор цепочек DPI bypass |

## Визуальный конструктор цепочек (Tab 11)

- Кастомный WPF Canvas с drag-and-drop узлов
- 8 типов узлов: Программа, Проверка, Zapret, ByeDPI, WARP, Задержка, Лог, Интернет
- Bezier-кривые для соединений между портами
- Zoom (колесо мыши) и pan (Alt+Left / Middle button)
- Визуальная обратная связь при выборе ноды (синяя рамка)
- Кнопки: удаление выбранной ноды, сброс вида
- Сохранение/загрузка цепочек в JSON файлы (`chains/*.chain.json`)
- Боковая палитра узлов со списком цепочек

## GPL v3 Compliance

| Файл | Назначение |
|------|------------|
| `LICENSE` | Полный текст GPL v3 |
| `NOTICE` | Атрибуция upstream и сторонних компонентов |
| `Directory.Build.props` | Централизованные метаданные лицензии |
| `BSDPI.Updater/Services/AppUpdaterService.cs` | Должен указывать на `mx57/BSDPI_AI`, НЕ на upstream |
| `README.md` / `README.en.md` | Отказ от ответственности (educational/research purpose) |

**⚠️ При fork:** AppUpdaterService.cs содержит URLs на релизы. Если форкать — заменить `klondike0x/BSDPI` на свой repo.

## Как работает ИИ-цикл

```
1. SyncBuiltins()        — сканирование engine/*.bat → реестр геномов
2. PickAndApplyInitial() — Thompson Sampling → выбрать лучшую стратегию
3. LoopAsync()           — цикл проверки каждые N минут:
   a. Capture() network fingerprint
   b. ProbeCurrent() — проверить сайты через текущую стратегию
   c. RecordBanditSuccess/Failure → обновить Alpha/Beta
   d. MaybeEvolve() — если прошло достаточно проб → эволюция
   e. SwitchToAlternative() — если 2+ провала подряд → смена стратегии
```

## Thompson Sampling (BanditSelector)

- **Alpha** = количество успехов, **Beta** = количество провалов
- Sample из Beta(Alpha, Beta) → вероятность успеха
- Epsilon-greedy: explorationPermil (‰) шанс на exploration
- Блокировка после провала: экспоненциальный backoff (300ms → 3s)
- Aggregated Beta: суммирование по всем сетям для новых стратегий

## Генетическая эволюция (StrategyEvolver)

- **Родители**: топ-6 по Wilson lower bound
- **Crossover**: случайный выбор параметров от обоих родителей
- **Мутации**: 12 типов (split pos, desync mode, fake ttl, engine switch, ...)
- **Валидация**: StrategyGenomeValidator + уникальность через GenomeSignature
- **GarbageCollect**: удаление худших если > MaxEvolvedStrategies

## Сетевой отпечаток (NetworkFingerprintProvider)

- Собирает: NIC ID, тип, IPv4, шлюз, DNS, подсеть
- SHA256 от нормализованной строки → Hash
- Label: "Ethernet/192.168.1.1" или "Wi-Fi/10.0.0.1"

## Файлы данных

| Файл | Описание |
|------|----------|
| `fluxroute-ai-registry.json` | Реестр геномов + bandit-состояние |
| `fluxroute-ai-history.jsonl` | История проб (JSONL, одна строка = один ProbeOutcome) |
| `engine/ai-evolved/*.bat` | Сгенерированные .bat файлы |
| `chains/*.chain.json` | Сохранённые цепочки DPI bypass |

## Настройки ИИ (AiSettings)

| Параметр | Дефолт | Описание |
|----------|--------|----------|
| Enabled | false | Включён ли ИИ-оркестратор |
| ExplorationRatePermil | 100 | Exploration в ‰ (10% = 100‰) |
| MaxEvolvedStrategies | 24 | Макс. эволюционированных стратегий |
| EvolutionIntervalMinutes | 60 | Интервал между эволюциями |
| MinProbesBeforeEvolve | 6 | Мин. проб перед эволюцией |
| KeepHistoryDays | 14 | Хранить историю N дней |
| UseHybridMode | false | Гибридный режим (zapret + byedpi) |
| ByeDpiSocksPort | 1080 | SOCKS5 порт для byedpi |
| AutoDeleteBelowScore | 60 | Автоудаление эволюционированных ниже этого % |

## Движки DPI

### Zapret (winws.exe)
- Классический DPI bypass через фрагментацию пакетов
- Параметры: --dpi-desync, --dpi-desync-split-pos, --dpi-desync-ttl, ...

### ByeDPI (ciadpi.exe)
- Альтернативный движок с SOCKS5 прокси
- Параметры: --disorder, --fake-tls, --tlsrec, --oob, ...
- Запуск через: `start /min "" ciadpi.exe <args>`

## Типы мутаций (StrategyEvolver)

### Zapret мутации (roll 0-7)
- 0: SplitPosSemantic → host/endhost/midsld/sniext/endsld
- 1: DesyncMode → split/fake/fakesplit/disorder/...
- 2: FakeTtl ± delta
- 3: FakeTlsMod → orig/rand/rndsni/dupsid/padencap
- 4: AutoTtl toggle
- 5: Engine switch to ByeDpi
- 6+: SplitPos numeric + разное

### ByeDpi мутации (roll 0-10)
- 0: SplitPosSemantic из кандидатов
- 1: DisorderPos из кандидатов
- 2: FakePos + FakeTtl
- 3: TlsrecPos
- 4: OobPos
- 5: FakeTtl ± delta
- 6: Md5sig toggle
- 7: FakeTlsMod
- 8: Auto mode (torst/ssl_err) + Timeout
- 9: ModHttp
- 10: Engine switch to Zapret (сброс ByeDpi-параметров)

## Сборка и тесты

```bash
dotnet restore BSDPI.slnx
dotnet build BSDPI.slnx
dotnet test BSDPI.slnx
dotnet run --project BSDPI
```

### Публикация релиза
```bash
dotnet publish BSDPI/BSDPI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

### Типовые конфликты типов
Проект использует WPF + WinForms одновременно. Всегда добавляйте алиасы:
```csharp
using Point = System.Windows.Point;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
```
