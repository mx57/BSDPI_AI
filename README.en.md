<div align="center">

<picture>
    <source media="(prefers-color-scheme: dark)" srcset="./assets/FluxRoute-white.svg">
    <source media="(prefers-color-scheme: light)" srcset="./assets/FluxRoute-dark.svg">
    <img width="600" alt="FluxRoute AI" src="./assets/FluxRoute-dark.svg" />
</picture>

# FluxRoute AI `v1.6.2`

**Language:** [🇷🇺 Русский](README.md) | 🇬🇧 English

### Smart DPI Bypass Automation with AI Orchestrator and Warp Support

[![Stars](https://img.shields.io/github/stars/mx57/FluxRoute_AI?style=for-the-badge&logo=github&color=FFD700)](https://github.com/mx57/FluxRoute_AI)
[![Version](https://img.shields.io/github/v/release/mx57/FluxRoute_AI?include_prereleases&sort=semver&logo=github&label=version&style=for-the-badge)](https://github.com/mx57/FluxRoute_AI/releases)
[![Downloads](https://img.shields.io/github/downloads/mx57/FluxRoute_AI/total?logo=github&label=downloads&style=for-the-badge)](https://github.com/mx57/FluxRoute_AI/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&style=for-the-badge)](https://dotnet.microsoft.com/)

---

**FluxRoute AI** is a modern Windows client for managing DPI bypass tools (Zapret, ByeDPI, Warp). Its key feature is a self-learning AI that analyzes your network and automatically selects working strategies.

</div>

---

## 🚀 What's New in v1.6.2

*   **Cloudflare Warp (WireGuard):** Full `warp-plus` integration. Works standalone or chained with Zapret/ByeDPI.
*   **Auto MTU Tuning:** AI now automatically selects the optimal MTU for Warp based on connection stability.
*   **New Engine Modes:**
    *   `Hybrid`: Intelligent switching between Zapret and ByeDPI.
    *   `Warp + Zapret / ByeDPI`: Parallel launch or Chained (Zapret via Warp SOCKS5).
*   **AI Improvements:** Fast Start logic and deeper parameter mutations (Desync, FakeResend).
*   **AI Caching:** Instant restoration of top strategies when switching networks.

---

## ✨ Core Features

| Feature | Description |
|------|----------|
| 🧠 **AI Orchestrator** | Thompson Sampling for strategy selection tailored to your ISP. |
| 🧬 **Genetic Evolution** | Creating new BAT profiles by crossing the best parameters. |
| 🛡️ **Warp / AmneziaWG** | Built-in Warp support to bypass IP-based blocks. |
| 🌐 **Network Fingerprint** | Per-network AI policy (Home / Work / Mobile). |
| 📊 **Wilson Scoring** | Mathematically precise ranking based on reliability. |
| 🔄 **Auto-Updates** | All engines (zapret, byedpi, warp) update automatically from GitHub. |

---

## 🛠 How It Works

```mermaid
flowchart TD
    A[🌐 Network] --> B{AI Orchestrator}
    B -->|Thompson Sampling| C[Pick Strategy]
    C --> D[Run: Zapret / ByeDPI / Warp]
    D --> E[Verify: YT, Discord, TG]
    E -->|Wilson Score| F[Update Rating]
    F -->|Evolution| G[Generate new BATs]
    G --> C
```

---

## 📸 Screenshots

<div align="center">
    <img src="https://github.com/user-attachments/assets/70dda58d-cbf3-43f8-b8ae-72b7fad3d88e" width="400" alt="Main UI" />
    <img src="https://github.com/user-attachments/assets/bf33cffb-6d56-4055-8f8e-8c807f57d9a7" width="400" alt="AI Stats" />
</div>

---

## ⚠️ Important Note

The project uses **WinDivert**. Some antiviruses may flag it (RiskTool or HackTool). This is common for traffic interception tools. Please add the application folder to exclusions.

---

## 🙏 Acknowledgments

*   **[klondike0x/FluxRoute](https://github.com/klondike0x/FluxRoute)** — Original project (v1.5.0).
*   **[bol-van/zapret](https://github.com/bol-van/zapret)** — The heart of the project.
*   **[hiddify/warp-plus](https://github.com/hiddify/warp-plus)** — Warp implementation.

---

<div align="center">

**[⭐ Star the repo](https://github.com/mx57/FluxRoute_AI) • [📥 Download](https://github.com/mx57/FluxRoute_AI/releases) • [💬 Support](https://github.com/mx57/FluxRoute_AI/issues)**

</div>
