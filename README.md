<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/FluxRoute-white.svg">
  <source media="(prefers-color-scheme: light)" srcset="./assets/FluxRoute-dark.svg">
  <img width="650" alt="FluxRoute AI Logo" src="./assets/FluxRoute-dark.svg" />
</picture>

# FluxRoute AI `v1.7.1`

### ⚡ Самообучающийся DPI-Bypass на Windows

**Объединяет Zapret, ByeDPI и Cloudflare Warp в единую адаптивную систему с ИИ-оркестратором.**

[🇬🇧 English](README.en.md) | [📥 Скачать](https://github.com/mx57/FluxRoute_AI/releases) | [🐛 Проблема](https://github.com/mx57/FluxRoute_AI/issues)

---

[![Stars](https://img.shields.io/github/stars/mx57/FluxRoute_AI?style=for-the-badge&logo=github&color=FFD700)](https://github.com/mx57/FluxRoute_AI)
[![Releases](https://img.shields.io/github/v/release/mx57/FluxRoute_AI?include_prereleases&sort=semver&logo=github&label=версия&style=for-the-badge&color=3FB950)](https://github.com/mx57/FluxRoute_AI/releases)
[![Downloads](https://img.shields.io/github/downloads/mx57/FluxRoute_AI/total?logo=github&label=загрузки&style=for-the-badge&color=4FC3F7)](https://github.com/mx57/FluxRoute_AI/releases)
[![.NET 10](https://img.shields.io/badge/.NET_10-512BD4?logo=dotnet&style=for-the-badge)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/GPLv3-blue.svg?style=for-the-badge)](./LICENSE)

</div>

---

## 🌟 Почему FluxRoute AI?

Обычные GUI для DPI-инструментов просто запускают процессы. **FluxRoute AI** **думает**. Блокировки постоянно меняются — то, что работало вчера, сегодня перестало. Наша система использует математические модели для автоматической адаптации к вашей сети и провайдеру.

> **Этот форк является родителем всех AI-функций, попавших в оригинальный FluxRoute.**

---

## 🛠 Ключевые возможности

| Категория | Возможности |
| :--- | :--- |
| **🔧 Движки** | Zapret (`winws.exe`), ByeDPI (`ciadpi.exe`), Cloudflare Warp (`warp-plus.exe`) |
| **⚡ Режимы** | Standalone · Hybrid · Warp · Warp+Zapret · Warp+ByeDPI · Chained (×2) · Bypass |
| **🧠 ИИ** | Thompson Sampling · Генетическая эволюция · Wilson Score · Fast Start · Авто-MTU |
| **🌐 Сеть** | Network Fingerprinting (политика на каждую сеть) · Авто-смена при смене сети |
| **🤖 Автоматизация** | Авто-регистрация Warp · Фоновый мониторинг · Авто-обновление с GitHub |
| **✈️ TG WS Proxy** | Telegram WebSocket прокси с авто-установкой и Cloudflare поддержкой |
| **🎮 Сервис** | Game Filter · IPSet · Auto-Tune · Управление службой Zapret |
| **🔄 Обновления** | 5 независимых каналов: движок, приложение, ByeDPI, Warp, TG Proxy |
| **📋 Домены** | Менеджер доменов (цели + исключения) · Синхронизация с winws.exe |
| **⚙️ Пресеты** | Сохранение конфигураций · Авто-переключение по процессу |
| **📊 Диагностика** | Проверка компонентов · Экспорт бандла · Единый лог-вьювер |

---

## 🧠 Искусственный Интеллект

### Thompson Sampling (Многорукие бандиты)

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

### Network Fingerprinting

- Сбор данных: тип сети, IP-адреса, шлюз, DNS-серверы, префиксы подсетей
- Хеш SHA-256 для идентификации сети
- **Своя политика ИИ для каждой сети** — работает на Wi-Fi дома и на мобильном интернете по-разному

### Fast Start

При запуске или смене сети мгновенно проверяет **3 лучших стратегии** для быстрого подбора.

### Wilson Score

Стратегии ранжируются по **нижней границе Уилсона** (95% доверительный интервал) — статистически строгая оценка качества.

---

## ⚡ Режимы работы

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

## ✈️ TG WS Proxy

Встроенный Telegram WebSocket прокси для обхода блокировок Telegram:

- **Авто-установка** — скачивает Python Embeddable, pip, cryptography и исходники прокси
- **Cloudflare Proxy** — проксирование трафика через Cloudflare
- **DC маппинг** — настройка IP-адресов Telegram DC
- **Deep Link** — открытие прокси одной кнопкой в Telegram
- **Авто-старт** при запуске приложения

---

## 🎮 Сервис и настройки

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

## 🔄 Обновления

| Компонент | Источник |
| :--- | :--- |
| Flowseal Zapret Engine | [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) |
| FluxRoute Application | [mx57/FluxRoute_AI](https://github.com/mx57/FluxRoute_AI) |
| ByeDPI (CIADPI) | [repo ByeDPI](https://github.com/) |
| Warp (WARP-PLUS) | [repo Warp-plus](https://github.com/) |
| TG WS Proxy | [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy) |

- Проверка при запуске (опционально)
- Авто-скачивание при первом запуске (если папка `engine/` пуста)
- Принудительная переустановка

---

## 📋 Домены и пресеты

### Менеджер доменов
- Два списка: **цели** (для обхода) и **исключения**
- Автоматическая синхронизация с `list-general-user.txt` для winws.exe
- Нормализация ввода (удаление протоколов, www, слэшей)

### Пресеты конфигурации
- Сохранение текущего профиля + GameFilter + IPSet
- **Авто-переключение** по запущенному процессу (обнаружение игр)
- Фоновый мониторинг каждые 3 секунды

---

## 📊 Единый лог-вьювер

- 8 категорий: Приложение, Оркестратор, Сканирование, Запуск, TG Proxy, Обновление, Сервис, Ошибки
- Поиск по тексту, фильтр только ошибки
- Автопрокрутка, копирование, сохранение в файл

---

## 🏗 Архитектура

```
FluxRoute.slnx
├── FluxRoute/              # WPF UI (11 вкладок, GitHub-dark тема)
│   ├── Views/              # MainWindow.xaml (2488 строк)
│   └── ViewModels/         # MVVM: Main, AI, Orchestrator, TgProxy, Service...
├── FluxRoute.AI/           # AI подсистема
│   ├── Services/           # AiOrchestrator, BanditSelector, StrategyEvolver
│   └── Math/               # WilsonScore
├── FluxRoute.Core/         # Движки и ядро
│   ├── Services/           # IDpiEngine, Zapret/ByeDpi/WarpEngine, Connectivity
│   └── Models/             # EngineProfile, StrategyGenome (40+ параметров)
├── FluxRoute.Updater/      # 5 каналов обновлений
└── FluxRoute.Core.Tests/   # Unit-тесты
```

**Стек:** .NET 10 · C# · WPF · CommunityToolkit.Mvvm · Microsoft.Extensions.DI · Serilog

---

## 🚀 Быстрый старт

1. Скачайте [`FluxRoute-v1.7.1-portable.zip`](https://github.com/mx57/FluxRoute_AI/releases/tag/v1.7.1)
2. Распакуйте в любую папку
3. Запустите `FluxRoute.exe` **от имени администратора**
4. Выберите профиль и нажмите **▶ Запустить**

> При первом запуске движок скачивается автоматически.

---

## ⚠️ Безопасность

Проект использует драйвер **WinDivert** для модификации сетевых пакетов.
- Это **не вирус** — инструмент системного администрирования
- Антивирусы могут пометить как `HackTool` / `RiskTool`
- **Решение:** добавьте папку в исключения антивируса

---

## 🙏 Благодарности

- **[klondike0x/FluxRoute](https://github.com/klondike0x/FluxRoute)** — базовая архитектура
- **[bol-van/zapret](https://github.com/bol-van/zapret)** — ядро для обхода DPI
- **[Flowseal](https://github.com/Flowseal)** — BAT-профили, TG Proxy, авто-обновления
- **[hiddify/warp-plus](https://github.com/hiddify/warp-plus)** — CLI реализация Warp

---

## 📅 Дорожная карта

- [ ] Интеграция Sing-Box (VLESS, Reality)
- [ ] P2P-обмен геномами между пользователями
- [ ] Визуализация трафика в реальном времени

---

<div align="center">

**Развивается сообществом для свободного интернета.**

[mx57](https://github.com/mx57) © 2026 · GPLv3

**[⭐ Ставь звезду, если проект помог!](https://github.com/mx57/FluxRoute_AI)**

</div>
