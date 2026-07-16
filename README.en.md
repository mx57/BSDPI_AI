<div align="center">

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./assets/BSDPI_AI-white.svg">
  <source media="(prefers-color-scheme: light)" srcset="./assets/BSDPI_AI-dark.svg">
  <img width="650" alt="BSDPI_AI Logo" src="./assets/BSDPI_AI-dark.svg" />
</picture>

<br/>

**ISP blocking you? BSDPI_AI finds a way.**

Self-learning DPI bypass system that **automatically finds and evolves** working strategies for your network.

[🇷🇺 Русский](README.md) · [📥 Download](https://github.com/mx57/BSDPI_AI/releases) · [🐛 Bug Report](https://github.com/mx57/BSDPI_AI/issues)

---

[![Stars](https://img.shields.io/github/stars/mx57/BSDPI_AI?style=flat-square&logo=github&color=FFD700)](https://github.com/mx57/BSDPI_AI)
[![Release](https://img.shields.io/github/v/release/mx57/BSDPI_AI?include_prereleases&sort=semver&logo=github&label=latest&style=flat-square&color=3FB950)](https://github.com/mx57/BSDPI_AI/releases)
[![Downloads](https://img.shields.io/github/downloads/mx57/BSDPI_AI/total?logo=github&label=downloads&style=flat-square&color=4FC3F7)](https://github.com/mx57/BSDPI_AI/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&style=flat-square)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/GPLv3-blue.svg?style=flat-square)](./LICENSE)

</div>

---

### Why does this matter?

DPI filters from ISPs evolve constantly. What worked yesterday gets blocked today. Manual profile switching is tedious and unreliable.

**BSDPI_AI solves this with AI:**
- Analyzes which strategies **actually work on your network**
- Auto-switches when current strategy fails
- **Creates new** bypass parameters via genetic evolution
- Remembers policies per Wi-Fi network and mobile connection

---

> ### Important: About the Origin of the AI Component
>
> **BSDPI_AI is a fork of [klondike0x/BSDPI](https://github.com/klondike0x/BSDPI) and is the parent of all AI features** that are now present in the original project — Thompson Sampling, Genetic Evolution, Network Fingerprinting, Wilson Score, Fast Start, and others.
>
> These features were developed and implemented in this repository, then incorporated into the original BSDPI. **The original author did not credit this fact** in his repository and did not list BSDPI_AI in the acknowledgments section.
>
> Furthermore, the original BSDPI author publicly states that forks "may be malicious" and "recommends against running them." **This claim is unfounded:**
>
> - The project is **fully open-source** (GPLv3) — all code is available for inspection
> - There is **no malicious code** in the repository — anyone can audit it
> - All dependencies are public NuGet packages with open-source code
> - Binary releases are built from source via CI/CD
> - The project contains **53 unit tests** and documentation for every component
>
> We encourage the community to verify code independently rather than relying on unsubstantiated claims.

---

## Key Features

| Category | Features |
| :--- | :--- |
| **Engines** | Zapret (`winws.exe`), ByeDPI (`ciadpi.exe`), Cloudflare Warp (`warp-plus.exe`) |
| **Modes** | Standalone · Hybrid · Warp · Warp+Zapret · Warp+ByeDPI · Chained (×2) · Bypass |
| **AI** | Thompson Sampling · Genetic Evolution · Wilson Score · Fast Start · Auto-MTU |
| **Network** | Network Fingerprinting (per-network policy) · Auto-switch on network change |
| **Automation** | Auto Warp registration · Background monitoring · Auto-update from GitHub |
| **TG WS Proxy** | Telegram WebSocket proxy with auto-install and Cloudflare support |
| **Service** | Game Filter · IPSet · Auto-Tune · Zapret Windows Service management |
| **Updates** | 5 independent channels: engine, app, ByeDPI, Warp, TG Proxy |
| **Domains** | Domain manager (targets + exclusions) · Sync with winws.exe |
| **Presets** | Save configurations · Auto-switch by running process |
| **Diagnostics** | Component checks · Diagnostic bundle export · Unified log viewer |
| **Chain Builder** | Visual drag-and-drop DPI bypass chain constructor |

---

## Artificial Intelligence

### Thompson Sampling (Multi-Armed Bandits)

The AI orchestrator analyzes strategy success rates and uses **Beta distributions** to balance:
- **Exploitation** — using the best proven strategy
- **Exploration** — periodically testing new strategies that may work better

Configurable `ExplorationRate` (‰) controls the balance.

### Genetic Evolution

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

## Operating Modes

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

## TG WS Proxy

Built-in Telegram WebSocket proxy for bypassing Telegram blocks:

- **Auto-install** — downloads Python Embeddable, pip, cryptography, and proxy source files
- **Cloudflare Proxy** — traffic proxying via Cloudflare
- **DC Mapping** — Telegram DC IP address configuration
- **Deep Link** — open proxy in Telegram with one button
- **Auto-start** on app launch

---

## Service & Settings

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

## Visual Chain Builder

Full drag-and-drop constructor for building DPI bypass pipelines:

- **8 node types**: Program, Check, Zapret, ByeDPI, WARP, Delay, Log, Internet
- **Bezier curves** for connections between ports
- **Zoom** (mouse wheel) and **pan** (Alt+LMB / middle button)
- **Visual feedback** on node selection (blue border)
- Save/load chains as JSON files (`chains/*.chain.json`)

---

## Updates

| Component | Source |
| :--- | :--- |
| Flowseal Zapret Engine | [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) |
| BSDPI_AI Application | [mx57/BSDPI_AI](https://github.com/mx57/BSDPI_AI) |
| ByeDPI (CIADPI) | ByeDPI repository |
| Warp (WARP-PLUS) | Warp-plus repository |
| TG WS Proxy | [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy) |

- Check on startup (optional)
- Auto-download on first launch (if `engine/` folder is empty)
- Force reinstall option

---

## Domains & Presets

### Domain Manager
- Two lists: **targets** (for bypass) and **exclusions**
- Auto-sync with `list-general-user.txt` for winws.exe
- Input normalization (strips protocols, www, trailing slashes)

### Config Presets
- Save current profile + GameFilter + IPSet settings
- **Auto-switch** by running process (game detection)
- Background monitoring every 3 seconds

---

## Unified Log Viewer

- 8 categories: App, Orchestrator, Scan, Launch, TG Proxy, Update, Service, Errors
- Text search, errors-only filter
- Auto-scroll, copy to clipboard, save to file

---

## Architecture

```
BSDPI_AI.slnx
├── BSDPI_AI/              — WPF GUI (MVVM, 11 tabs)
│   ├── Views/             — MainWindow.xaml
│   └── ViewModels/        — Main, AI, Orchestrator, TgProxy, Service...
├── BSDPI_AI.AI/           — AI subsystem
│   ├── Services/          — AiOrchestrator, BanditSelector, StrategyEvolver
│   └── Math/              — WilsonScore
├── BSDPI_AI.Core/         — Engine abstraction
│   ├── Services/          — IDpiEngine, Zapret/ByeDpi/WarpEngine, Connectivity
│   └── Models/            — EngineProfile, StrategyGenome (40+ parameters)
├── BSDPI_AI.Updater/      — 5 update channels
└── BSDPI_AI.Core.Tests/   — Unit tests (53 tests)
```

**Stack:** .NET 10 · C# · WPF · CommunityToolkit.Mvvm · Microsoft.Extensions.DI · Serilog · LiveChartsCore

---

## GUI Tabs

| # | Tab | Description |
|---|-----|-------------|
| 0 | Main | Status, start/stop, domains |
| 1 | TG Proxy | Telegram WebSocket Proxy |
| 2 | Orchestrator | Classic orchestrator (profile rating) |
| 3 | AI | AI orchestrator, strategies, evolution, Wilson Score |
| 4 | Update | Update engine/ from GitHub |
| 5 | Diagnostics | File, process, connectivity checks |
| 6 | Service | Game Filter, IPSet, Auto-Tune, zapret service |
| 7 | About | Project information |
| 8 | Logs | Unified log viewer (8 categories) |
| 9 | ByeDPI | ByeDPI settings (SOCKS5, ports, parameters) |
| 10 | WARP | Cloudflare Warp config generator (WireGuard) |
| 11 | Chain Builder | Visual DPI bypass chain constructor |

---

## AI Settings

| Parameter | Default | Description |
|-----------|:-------:|-------------|
| `Enabled` | `false` | Enable AI orchestrator |
| `ExplorationRatePermil` | `100` | Exploration in ‰ (100‰ = 10%) |
| `MaxEvolvedStrategies` | `24` | Max evolved strategies |
| `EvolutionIntervalMinutes` | `60` | Evolution interval |
| `MinProbesBeforeEvolve` | `6` | Min probes before evolution |
| `KeepHistoryDays` | `14` | History retention (days) |
| `FastStartEnabled` | `true` | Fast start on launch |
| `ParetoEnabled` | `true` | Pareto optimization |
| `ElitismEnabled` | `true` | Elitism in genetic evolution |
| `AutoDeleteBelowScore` | `60` | Auto-delete strategies below threshold |

---

## Build & Run

**Requirements:** .NET 10 SDK, Windows 10/11 x64, administrator privileges

```bash
dotnet restore BSDPI_AI.slnx
dotnet build BSDPI_AI.slnx
dotnet run --project BSDPI_AI
```

### Tests

```bash
dotnet test BSDPI_AI.slnx
```

### Publish Release

```bash
dotnet publish BSDPI_AI/BSDPI_AI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

## Security

This project uses the **WinDivert** driver for packet modification.
- **Not a virus** — it's a system administration tool
- Antivirus software may flag it as `HackTool` / `RiskTool`
- **Solution:** add the folder to your antivirus exclusions

---

## Acknowledgments

- **[klondike0x/BSDPI](https://github.com/klondike0x/BSDPI)** — base architecture
- **[bol-van/zapret](https://github.com/bol-van/zapret)** — DPI bypass core
- **[Flowseal](https://github.com/Flowseal)** — BAT profiles, TG Proxy, auto-updates
- **[hiddify/warp-plus](https://github.com/hiddify/warp-plus)** — Warp CLI implementation
- **[basil00/WinDivert](https://github.com/basil00/WinDivert)** — packet modification driver

---

## Roadmap

- [ ] Sing-Box integration (VLESS, Reality)
- [ ] P2P genome sharing between users
- [ ] Real-time traffic visualization
- [ ] Cloud AI Sync — fetch ready-made genomes from the cloud

---

## License

This project is licensed under [GPL-3.0](LICENSE).

---

<div align="center">

**Developed by the community for a free internet.**

[mx57](https://github.com/mx57) © 2026 · GPLv3

**[⭐ Star this repo if it helped you!](https://github.com/mx57/BSDPI_AI)**

</div>

---

> **Disclaimer.** **BSDPI_AI** is educational and research software designed for studying network technologies. This software **is not** a tool for violating applicable laws. Use of this software must comply with the applicable laws of the user's jurisdiction. The author assumes no responsibility for any consequences arising from the use of this software.
