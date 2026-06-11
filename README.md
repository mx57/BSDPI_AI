<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/FluxRoute-white.svg">
  <source media="(prefers-color-scheme: light)" srcset="./assets/FluxRoute-dark.svg">
  <img width="650" alt="FluxRoute AI Logo" src="./assets/FluxRoute-dark.svg" />
</picture>

# FluxRoute AI `v1.7.0-beta`

### ⚡ Интеллектуальный швейцарский нож для обхода DPI на Windows

**Самообучающаяся система, объединяющая Zapret, ByeDPI, Warp и Sing-Box в единый адаптивный механизм.**

[🇬🇧 English Version](README.en.md) | [📥 Скачать релиз](https://github.com/mx57/FluxRoute_AI/releases) | [🐛 Сообщить о проблеме](https://github.com/mx57/FluxRoute_AI/issues)

---

[![Stars](https://img.shields.io/github/stars/mx57/FluxRoute_AI?style=for-the-badge&logo=github&color=FFD700)](https://github.com/mx57/FluxRoute_AI)
[![Releases](https://img.shields.io/github/v/release/mx57/FluxRoute_AI?include_prereleases&sort=semver&logo=github&label=версия&style=for-the-badge&color=3FB950)](https://github.com/mx57/FluxRoute_AI/releases)
[![Downloads](https://img.shields.io/github/downloads/mx57/FluxRoute_AI/total?logo=github&label=загрузки&style=for-the-badge&color=4FC3F7)](https://github.com/mx57/FluxRoute_AI/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&style=for-the-badge)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/Лицензия-GPLv3-blue.svg?style=for-the-badge)](./LICENSE)

</div>

---

## 🌟 Почему FluxRoute AI?

Обычные GUI для DPI-инструментов просто запускают процессы. **FluxRoute AI** идёт дальше — он **думает**. Блокировки постоянно меняются, и то, что работало вчера, может перестать работать сегодня. Наша система использует математические модели для автоматической адаптации к вашей сети.

### 🧠 Искусственный Интеллект (Thompson Sampling)
Вместо перебора профилей вручную, ИИ-оркестратор анализирует успешность каждой попытки соединения. Он использует алгоритм **Многоруких бандитов (Multi-armed bandits)** для баланса между использованием надежных стратегий и поиском новых.

### 🧬 Генетическая Эволюция (StrategyEvolver)
Система буквально "выращивает" новые BAT-файлы. Она скрещивает параметры самых успешных стратегий, применяет случайные мутации и проверяет результат. Выживают только лучшие.

---

## 🛠 Ключевые возможности

| Категория | Возможности |
| :--- | :--- |
| **Поддержка ядер** | Zapret, ByeDPI, Cloudflare Warp, **Sing-Box** (VLESS, Reality). |
| **Режимы работы** | Standalone, Hybrid, Parallel, Chained (через SOCKS5/Tunstall). |
| **Интеллект** | Thompson Sampling, Wilson Lower Bound, Fast Start, Cloud AI Sync. |
| **Сеть** | Network Fingerprinting, авто-подбор MTU, поддержка AmneziaWG. |
| **Автоматизация** | Полный цикл авто-обновлений всех движков с GitHub. |

---

## 🚀 Новое в версии 1.7.0-alpha

> [!TIP]
> Мы расширяем границы — теперь FluxRoute AI умеет работать с современными протоколами и делиться опытом в облаке.

- **📦 Интеграция Sing-Box:** Поддержка самого универсального ядра. Теперь можно использовать VLESS, Reality и Shadowsocks прямо внутри FluxRoute.
- **☁️ Cloud AI Sync (Beta):** Инфраструктура для обмена успешными "геномами" стратегий между пользователями (анонимно).
- **🔄 Engine 4.0:** ИИ теперь управляет четырьмя движками одновременно, выбирая наиболее эффективный для текущих условий.
- **⚡ Оптимизация ядра:** Улучшено быстродействие реестра стратегий и системы логирования.

---

## 📸 Галерея интерфейса

<div align="center">
  <table border="0">
    <tr>
      <td><img src="https://github.com/user-attachments/assets/70dda58d-cbf3-43f8-b8ae-72b7fad3d88e" width="400" alt="Main UI" /><br/><p align="center"><i>Главный экран управления</i></p></td>
      <td><img src="https://github.com/user-attachments/assets/bf33cffb-6d56-4055-8f8e-8c807f57d9a7" width="400" alt="AI Stats" /><br/><p align="center"><i>Статистика ИИ и Эволюция</i></p></td>
    </tr>
  </table>
</div>

---

## 📅 Дорожная карта (Future)

- [x] **Интеграция Sing-Box:** Реализовано в v1.7.0.
- [x] **Cloud AI Sync:** Реализовано в v1.7.0.
- [ ] **Advanced YouTube Probing:** Проверка скорости буферизации видео (4K/8K) для выбора быстрейшего профиля.
- [ ] **Mobile Remote:** Управление десктопным клиентом через Telegram-бота.

---

## 🙏 Благодарности и Лицензия

Этот проект является свободным ПО и распространяется под лицензией **GNU GPLv3**.

- **[klondike0x/FluxRoute](https://github.com/klondike0x/FluxRoute)** — Оригинальный автор и архитектор.
- **[bol-van/zapret](https://github.com/bol-van/zapret)** — Ядро проекта.
- **[SagerNet/sing-box](https://github.com/SagerNet/sing-box)** — За универсальный движок.

---

<div align="center">

**Развивается сообществом для свободного интернета.**

[mx57](https://github.com/mx57) © 2026. Лицензия GPLv3.

**[⭐ Ставь звезду, если проект тебе помог!](https://github.com/mx57/FluxRoute_AI)**

</div>
