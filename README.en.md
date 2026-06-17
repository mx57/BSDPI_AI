<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/FluxRoute-white.svg">
  <source media="(prefers-color-scheme: light)" srcset="./assets/FluxRoute-dark.svg">
  <img width="650" alt="FluxRoute AI Logo" src="./assets/FluxRoute-dark.svg" />
</picture>

# FluxRoute AI `v1.7.1`

### ⚡ Self-Learning DPI Bypass for Windows

**Unifies Zapret, ByeDPI, and Cloudflare Warp into a single adaptive system with an AI orchestrator.**

[🇷🇺 Русский](README.md) | [📥 Download](https://github.com/mx57/FluxRoute_AI/releases) | [🐛 Report Issue](https://github.com/mx57/FluxRoute_AI/issues)

---

[![Stars](https://img.shields.io/github/stars/mx57/FluxRoute_AI?style=for-the-badge&logo=github&color=FFD700)](https://github.com/mx57/FluxRoute_AI)
[![Releases](https://img.shields.io/github/v/release/mx57/FluxRoute_AI?include_prereleases&sort=semver&logo=github&label=version&style=for-the-badge&color=3FB950)](https://github.com/mx57/FluxRoute_AI/releases)
[![Downloads](https://img.shields.io/github/downloads/mx57/FluxRoute_AI/total?logo=github&label=downloads&style=for-the-badge&color=4FC3F7)](https://github.com/mx57/FluxRoute_AI/releases)
[![.NET 10](https://img.shields.io/badge/.NET_10-512BD4?logo=dotnet&style=for-the-badge)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/GPLv3-blue.svg?style=for-the-badge)](./LICENSE)

</div>

---

## 🌟 Why FluxRoute AI?

Typical GUIs for DPI tools just launch processes. **FluxRoute AI** **thinks**. Censorship and DPI filters change constantly — what worked yesterday may fail today. Our system uses mathematical models to automatically adapt to your specific ISP and network.

> **This fork is the parent of all AI features that made it into the original FluxRoute release.**

---

## 🛠 Key Features

| Category | Features |
| :--- | :--- |
| **🔧 Engines** | Zapret (`winws.exe`), ByeDPI (`ciadpi.exe`), Cloudflare Warp (`warp-plus.exe`) |
| **⚡ Modes** | Standalone · Hybrid · Warp · Warp+Zapret · Warp+ByeDPI · Chained (×2) · Bypass |
| **🧠 AI** | Thompson Sampling · Genetic Evolution · Wilson Score · Fast Start · Auto-MTU |
| **🌐 Network** | Network Fingerprinting (per-network policy) · Auto-switch on network change |
| **🤖 Automation** | Auto Warp registration · Background monitoring · Auto-update from GitHub |
| **✈️ TG WS Proxy** | Telegram WebSocket proxy with auto-install and Cloudflare support |
| **🎮 Service** | Game Filter · IPSet · Auto-Tune · Zapret Windows Service management |
| **🔄 Updates** | 5 independent channels: engine, app, ByeDPI, Warp, TG Proxy |
| **📋 Domains** | Domain manager (targets + exclusions) · Sync with winws.exe |
| **⚙️ Presets** | Save configurations · Auto-switch by running process |
| **📊 Diagnostics** | Component checks · Diagnostic bundle export · Unified log viewer |

---

## 🧠 Artificial Intelligence

### Thompson Sampling (Multi-Armed Bandits)

The AI orchestrator analyzes strategy success rates and uses **Beta distributions** to balance:
- **Exploitation** — using the best proven strategy
- **Exploration** — periodically testing new strategies that may work better

Configurable `ExplorationRate` (‰) controls the balance.

### 🧬 Genetic Evolution

The system "grows" new BAT profiles:
1. **Crossover** of two best strategies' parameters
2. **Mutation** of 15 parameter types (split, desync, fake-TTL, fake-TLS, fooling, MTU, etc.)
3. **Validation** and **deduplication** via `GenomeSignature`
4. **Survival** of the fittest — weak strategies are auto-deleted

### Network Fingerprinting

- Data collected: network interface type, IPs, gateway, DNS servers, subnet prefixes
- SHA-256 hash for network identification
- **Per-network AI policy** — works differently on home Wi-Fi vs mobile internet

### Fast Start

On launch or network change, instantly probes the **top 3 strategies** for quick selection.

### Wilson Score

Strategies ranked by **Wilson lower bound** (95% confidence interval) — statistically rigorous quality estimation.

---

## ⚡ Operating Modes

| Mode | Description | ISP Evasion Difficulty |
| :--- | :--- | :--- |
| **Zapret** | Primary DPI-bypass engine | Low |
| **ByeDPI** | Alternative DPI-bypass engine | Low |
| **Warp** | Cloudflare WireGuard VPN | Medium |
| **Hybrid** | Zapret + ByeDPI parallel, smart switching | High |
| **Warp+Zapret** | Warp + Zapret parallel | High |
| **Warp+ByeDPI** | Warp + ByeDPI parallel | High |
| **Warp→Zapret Chained** | Zapret tunneled through Warp SOCKS5 | **Maximum** |
| **Warp→ByeDPI Chained** | ByeDPI tunneled through Warp SOCKS5 | **Maximum** |
| **Bypass** | No protection, pass-through | — |

---

## ✈️ TG WS Proxy

Built-in Telegram WebSocket proxy for bypassing Telegram blocks:

- **Auto-install** — downloads Python Embeddable, pip, cryptography, and proxy source files
- **Cloudflare Proxy** — traffic proxying via Cloudflare
- **DC Mapping** — Telegram DC IP address configuration
- **Deep Link** — open proxy in Telegram with one button
- **Auto-start** on app launch

---

## 🎮 Service & Settings

### Game Filter
Port range expansion (1024-65535) for game DPI bypass. Modes: TCP+UDP / TCP / UDP.

### IPSet
IP-based filtering. Three modes: loaded / disabled / all addresses. Download latest list from GitHub.

### Auto-Tune
Automated testing of **12 IPSet × GameFilter combinations**. Finds optimal settings by speed and success rate.

### Hosts File
Check and update system hosts file from Flowseal GitHub repository.

### Service Management
Install / stop Zapret Windows Service.

---

## 🔄 Updates

| Component | Source |
| :--- | :--- |
| Flowseal Zapret Engine | [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) |
| FluxRoute Application | [mx57/FluxRoute_AI](https://github.com/mx57/FluxRoute_AI) |
| ByeDPI (CIADPI) | ByeDPI repository |
| Warp (WARP-PLUS) | Warp-plus repository |
| TG WS Proxy | [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy) |

- Check on startup (optional)
- Auto-download on first launch (if `engine/` folder is empty)
- Force reinstall option

---

## 📋 Domains & Presets

### Domain Manager
- Two lists: **targets** (for bypass) and **exclusions**
- Auto-sync with `list-general-user.txt` for winws.exe
- Input normalization (strips protocols, www, trailing slashes)

### Config Presets
- Save current profile + GameFilter + IPSet settings
- **Auto-switch** by running process (game detection)
- Background monitoring every 3 seconds

---

## 📊 Unified Log Viewer

- 8 categories: App, Orchestrator, Scan, Launch, TG Proxy, Update, Service, Errors
- Text search, errors-only filter
- Auto-scroll, copy to clipboard, save to file

---

## 🏗 Architecture

```
FluxRoute.slnx
├── FluxRoute/              # WPF UI (11 tabs, GitHub-dark theme)
│   ├── Views/              # MainWindow.xaml (2488 lines)
│   └── ViewModels/         # MVVM: Main, AI, Orchestrator, TgProxy, Service...
├── FluxRoute.AI/           # AI subsystem
│   ├── Services/           # AiOrchestrator, BanditSelector, StrategyEvolver
│   └── Math/               # WilsonScore
├── FluxRoute.Core/         # Engine abstraction
│   ├── Services/           # IDpiEngine, Zapret/ByeDpi/WarpEngine, Connectivity
│   └── Models/             # EngineProfile, StrategyGenome (40+ parameters)
├── FluxRoute.Updater/      # 5 update channels
└── FluxRoute.Core.Tests/   # Unit tests
```

**Stack:** .NET 10 · C# · WPF · CommunityToolkit.Mvvm · Microsoft.Extensions.DI · Serilog

---

## 🚀 Quick Start

1. Download [`FluxRoute-v1.7.1-portable.zip`](https://github.com/mx57/FluxRoute_AI/releases/tag/v1.7.1)
2. Extract to any folder
3. Run `FluxRoute.exe` **as Administrator**
4. Select a profile and click **▶ Start**

> The engine downloads automatically on first launch.

---

## ⚠️ Security

This project uses the **WinDivert** driver for packet modification.
- **Not a virus** — it's a system administration tool
- Antivirus software may flag it as `HackTool` / `RiskTool`
- **Solution:** add the folder to your antivirus exclusions

---

## 🙏 Acknowledgments

- **[klondike0x/FluxRoute](https://github.com/klondike0x/FluxRoute)** — base architecture
- **[bol-van/zapret](https://github.com/bol-van/zapret)** — DPI bypass core
- **[Flowseal](https://github.com/Flowseal)** — BAT profiles, TG Proxy, auto-updates
- **[hiddify/warp-plus](https://github.com/hiddify/warp-plus)** — Warp CLI implementation

---

## 📅 Roadmap

- [ ] Sing-Box integration (VLESS, Reality)
- [ ] P2P genome sharing between users
- [ ] Real-time traffic visualization

---

<div align="center">

**Developed by the community for a free internet.**

[mx57](https://github.com/mx57) © 2026 · GPLv3

**[⭐ Star this repo if it helped you!](https://github.com/mx57/FluxRoute_AI)**

</div>
