<p align="center">
  <img src="docs/cover.png" alt="E-Helper — Casper Excalibur Gaming Laptop Control Center" width="100%">
</p>

# E-Helper — Casper Excalibur Control Center

<p align="center">
  <a href="https://github.com/berkevnl/E-Helper/releases/latest"><img src="https://img.shields.io/github/v/release/berkevnl/E-Helper?style=flat-square&color=0078D4&label=Version" alt="Version"></a>
  <a href="https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper-win-x64.zip"><img src="https://img.shields.io/badge/Download-ZIP_Package_(~64_MB)-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Direct Download ZIP"></a>
  <a href="https://github.com/berkevnl/E-Helper/blob/main/LICENSE"><img src="https://img.shields.io/badge/License-GPL--3.0-green.svg?style=flat-square" alt="License"></a>
  <a href="#-why-e-helper-key-advantages"><img src="https://img.shields.io/badge/RAM-5--10_MB_Tray_/_30--50_MB_Active-success?style=flat-square" alt="RAM Footprint"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/8.0"><img src="https://img.shields.io/badge/.NET-8.0_Windows_x64-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8"></a>
  <a href="README.tr.md"><img src="https://img.shields.io/badge/Dil-T%C3%BCrk%C3%A7e-red?style=flat-square" alt="Türkçe Döküman"></a>
</p>

---

**E-Helper** is an ultra-lightweight, dedicated open-source hardware management utility engineered specifically for **Casper Excalibur** series gaming laptops. It replaces bloated, slow, and resource-heavy vendor software (*Excalibur Control Center*) with a single, standalone portable executable requiring zero installation.

Inspired by the acclaimed **G-Helper** philosophy, E-Helper communicates directly with the laptop's Embedded Controller (EC) via ACPI WMI SMI commands and dedicated NVIDIA NVML APIs. It provides an instantaneous, responsive flyout control panel with **zero background services** and **zero telemetry tracking**.

<p align="center">
  <a href="https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper-win-x64.zip">
    <img src="https://img.shields.io/badge/DOWNLOAD_LATEST_RELEASE-ZIP_PACKAGE_(~64_MB)-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Download E-Helper ZIP Package" height="46">
  </a>
  <br>
  <sub><i>Lightweight compressed portable package (~64 MB) — Extract and run anywhere without installation.<br>For uncompressed standalone executables and release notes, visit <a href="https://github.com/berkevnl/E-Helper/releases/latest">GitHub Releases</a>.</i></sub>
</p>

---

## 📊 OEM Control Center vs. E-Helper

| Feature / Criterion | Official OEM Control Center | E-Helper (v1.3.1) |
| :--- | :---: | :---: |
| **RAM Footprint (Idle / System Tray)** | 150 MB – 350 MB (Often leaks up to 450 MB+) | **5 MB – 10 MB** |
| **RAM Footprint (Active Window / Flyout)** | 250 MB – 500 MB | **30 MB – 50 MB** (Auto-trimmed via Working Set API) |
| **Active Background Services & Daemons** | 4 – 6 persistent background services (`ControlCenter.exe`, `FnHotkeyUtility`, `OSDService`, `GCUBridge`) | **0 (Zero background services)** |
| **Telemetry & Analytics Tracking** | Background telemetry & usage analytics enabled | **0 (Strictly local, zero analytics / tracking)** |
| **Flyout Launch Latency** | 3 – 6+ seconds (heavy web/hybrid framework) | **Instant (< 100 ms flyout popup)** |
| **Installation Requirement** | Multi-step installer, background driver hooks, registry overhead | **Single portable executable (Zero installation)** |
| **Reboot Settings Persistence** | Frequently loses custom fan curves and RGB lighting after reboot or sleep | **100% resilient post-boot sync via delayed EC re-sync** |
| **Hardware Communication Architecture** | Bloated middleman service stack and background IPC | **Direct ACPI WMI SMI (`0xFA00` / `0xFB00`) & NVIDIA NVML** |
| **Keyboard Lighting Control** | Basic vendor presets | **3-Zone Custom RGB, High-Refresh Cycle (70 FPS), Left-to-Right Rainbow Wave, 3-Tier Speed Slider** |
| **Software Updates** | Cumbersome manual downloads and installers | **1-click silent background check, auto-download & restart** |

---

## ⚡ Why E-Helper? (Key Advantages)

1. **Engineered Exclusively for Excalibur:** Built from the ground up tailored to Casper Excalibur motherboard layouts and Quanta EC registers without generic bloated abstractions.
2. **Microscopic Resource Consumption:** Consumes merely **5 – 10 MB RAM** while idling in the System Tray. Even when the full UI is open, working memory remains capped at **30 – 50 MB** thanks to aggressive automated working-set memory trimming.
3. **Pure Zero-Bloat Operation:** No background services, no scheduled analytics daemons, and zero CPU cycles consumed when idle.
4. **Permanent Settings Persistence:** Power profiles, keyboard lighting modes, animation speed, brightness levels, and zone colors are seamlessly retained across cold boots and reboots via Windows Task Scheduler integration and delayed post-boot EC re-synchronization.
5. **Direct Hardware Bridge:** Issues direct SMI read (`0xFA00`) and write (`0xFB00`) commands to the Embedded Controller and reads die thermals directly from `nvml.dll`, eliminating intermediary vendor daemons.
6. **Instant-Access System Tray Flyout:** Docks discreetly in the Windows Notification Area. A single click presents an elegant flyout panel that can also be detached and freely moved anywhere on screen.
7. **Bilingual & Dual Theme Architecture:** Switch instantly between Turkish and English, as well as authentic Matte Dark and High-Contrast Light themes with zero application restarts.

---

## 🎯 Features & Capabilities

### 1. ⚡ Power & Performance Profiles
* **Eco (Office / Tasarruf):** Lowers fan acoustic profile, maximizes battery endurance, and minimizes thermal dissipation for office or quiet workflows.
* **Balanced (Gaming / Dengeli):** Dynamic fan curve and balanced thermal envelope calibrated for daily multitasking and gaming sessions.
* **Performance (Turbo / Performans):** Unlocks peak fan RPM, elevates thermal headroom, and sustains maximum CPU/GPU boost clock frequencies.
* *Integrated with native Windows power schemes (`PowerSetActiveScheme`) for instant system synchronization.*

### 2. 📊 Real-Time Hardware Telemetry
* **Die Thermals & Fan Tachometers:** Live CPU die temperatures (°C) and tachometer fan speeds (RPM).
* **Direct NVIDIA NVML Interop:** Directly queries `nvml.dll` to read authentic die temperatures and dedicated fan RPM from NVIDIA GeForce RTX/GTX GPUs, bypassing uncalibrated or static EC dummy registers.
* **System Resource Monitor:** Real-time system RAM allocation (used / total GB and percentage) and primary storage (C:) drive capacity.
* **Live System Tray Tooltip:** Hovering over the notification tray icon reveals instantaneous CPU/GPU thermals and fan RPM without needing to open the flyout window.

### 3. 🌈 Keyboard RGB Lighting Engine
* **6 Lighting Modes:**
  * **Static (Sabit):** Uniform single color across all physical keyboard zones.
  * **Breathing (Nefes):** Native hardware-level smooth breathing cycle with authentic fade to complete darkness and gentle return.
  * **Dynamic (Dinamik):** Hardware-native continuous color sequence.
  * **Cycle (Döngü):** Synchronized high-refresh (~70 FPS) full-spectrum cycle where all keys transition in unison through the 360° color spectrum without discrete color jumps or stepping artifacts.
  * **Rainbow Wave (Gökkuşağı):** Multi-zone spectrum wave flowing smoothly from left to right (WASD → Center → Numpad).
  * **3-Zone Custom RGB (3-Bölge):** Independent hex color customization for each physical zone.
* **Interactive 2D 3-Zone Map:** Visual bird's-eye schematic providing direct, clickable color assignment to Left (WASD), Center, and Right (Numpad) keyboard clusters.
* **Dynamic 3-Tier Speed Slider:** Seamlessly regulate animation speeds (**Slow**, **Medium**, **Fast**) for Cycle and Rainbow Wave modes, with live status updates reflected in the header (`Brightness: 100% - Fast`).
* **Windows 11-Style Color Picker:** 2D HSV color canvas, 1D hue slider, Hex code input field, RGB numeric controls, and curated quick swatches.
* **Hardware Brightness Steps:**
  * `0` : **Off (0%)** — Powers down keyboard LED power delivery planes.
  * `1` : **50% Brightness** — Balanced ambient illumination.
  * `2` : **100% Brightness** — Full maximum illumination.
  * *Supports direct click-to-position on the slider track.*
* **Cold-Boot Resilience:** Re-synchronizes lighting mode, brightness, speed, and custom colors on system boot.

### 4. 🎨 UI & Visual Ergonomics
* **Modern Typography:** High-clarity Google Sans and system font stacks rendered with ClearType and Display subpixel rendering modes for crisp legibility.
* **100% Vector Iconography:** Custom vector icons across all mode buttons, header cards, and controls with zero pixelation or DPI scaling blur.
* **Instant Theme Toggle:** One-click toggle between Matte Dark and Crisp Light themes.
* **Non-Blocking Asynchronous UI:** Hardware I/O runs entirely off the UI thread to guarantee stutter-free interactions.

### 5. 🔄 Seamless In-App Auto-Updater
* **Zero-Delay Startup Check:** Queries GitHub Releases silently in the background on startup with 0 ms impact on application launch.
* **Interactive Status Indicator:** Displays version status in the bottom-left corner (`Version: 1.3.1` when up to date, red `Download: 1.3.0 → 1.3.1` when an update is available).
* **One-Click Self-Update:** Automatically downloads the latest standalone package, closes the running process, swaps the binary, and restarts the updated application instantly.

---

## 💻 Hardware Compatibility

E-Helper is built specifically for **Casper Excalibur** laptops utilizing Quanta ACPI/WMI Embedded Controller platforms:

### ✅ Verified on Physical Hardware
> Tested and confirmed fully functional (power profiles, telemetry readouts, fan tachometers, and RGB keyboard lighting):
* **Excalibur G770** (Tested & Verified)
* **Excalibur G850 / G860** (Tested & Verified)
* **Excalibur G870** (Tested & Verified)
* **Excalibur G911** (Tested & Verified)

### 🔍 Architecture Compatible
Designed to work across Casper Excalibur models sharing Quanta ACPI/WMI command structures:
* **Excalibur G780**
* **Excalibur G900**
* **Excalibur G920**

---

## 🔬 Architecture & How It Works

```
┌────────────────────────────────────────────────────────────────────────┐
│                        E-Helper (WPF / .NET 8)                         │
│            Flyout UI  •  System Tray  •  Configuration Engine          │
└───────────────────┬────────────────────────────────┬───────────────────┘
                    │                                │
                    ▼ (ACPI WMI SMI)                 ▼ (NVIDIA NVML)
        ┌────────────────────────┐      ┌────────────────────────┐
        │  Casper / Quanta EC    │      │  NVIDIA Dedicated GPU  │
        │  - Power Profiles      │      │  - True Die Temp       │
        │  - Fan Tachometers     │      │  - Dedicated Fan RPM   │
        │  - RGB Keyboard Zones  │      └────────────────────────┘
        └────────────────────────┘
```

1. **WMI ACPI SMI Bridge:** Communicates with the hardware Embedded Controller through the Windows WMI namespace (`ROOT\WMI`). Hardware commands are packed into standardized `SMI_STRUCT_S` packets:
   * `0xFA00`: Command Read (thermal telemetry and fan RPM tachometers).
   * `0xFB00`: Command Write (performance profile switches, fan speed overrides, LED lighting packets).
   * Subfunctions: `0x0100` (LED lighting), `0x0200` (CPU/GPU thermals), `0x0205` (Fan speeds), `0x0300` (Power modes).
2. **NVML Direct Interop:** Loads `nvml.dll` dynamically to read accurate thermal telemetry directly from NVIDIA GeForce GPU driver interfaces, avoiding uncalibrated motherboard sensors.
3. **Elevated Windows Task Scheduler:** Configures an elevated scheduled task with `HighestAvailable` privileges, enabling seamless startup on Windows logon without triggering UAC elevation prompts.
4. **Working-Set Memory Trimming:** Employs Windows `SetProcessWorkingSetSize` APIs (`MemoryOptimizer`) to return unneeded physical memory pages to the OS whenever the window closes to the tray.

---

## 📂 Project Structure

```
ehelper/
├── src/
│   └── EHelper/
│       ├── Core/                      # Domain interfaces, enums and data contracts
│       │   ├── Enums/                 # PowerMode, LightingMode, BrightnessStep, ThemeMode
│       │   ├── Interfaces/            # IHardwareProvider, ISmiHardwareProvider contracts
│       │   └── Models/                # HardwareTelemetry, HardwareCapabilities models
│       ├── Hardware/                  # Low-level hardware abstraction layer
│       │   ├── Casper/                # ACPI WMI SMI and NVIDIA NVML implementation
│       │   │   ├── ExcaliburBridge.cs     # Motherboard EC & WMI SMI communication bridge
│       │   │   ├── ExcaliburSmi.cs        # SMI_STRUCT_S interop structures and P/Invoke
│       │   │   ├── NvmlHelper.cs          # Direct NVIDIA NVML GPU telemetry integration
│       │   │   └── ExcaliburPowerPlan.cs  # Windows Power Scheme synchronization
│       │   └── Simulation/            # Mock hardware provider for development/testing
│       ├── Services/                  # Application runtime services
│       │   ├── HardwareMonitorService.cs  # Asynchronous background telemetry polling loop
│       │   ├── ConfigManager.cs           # User configuration persistence (config.json)
│       │   ├── StartupManager.cs          # Windows Task Scheduler startup integration
│       │   ├── UpdateService.cs           # GitHub Releases silent auto-updater
│       │   ├── MemoryOptimizer.cs         # Automated RAM working-set memory trimmer
│       │   └── LocalizationManager.cs     # Dynamic Turkish/English runtime localization
│       ├── UI/                        # Presentation layer (WPF / XAML)
│       │   ├── MainWindow.xaml            # Modern flyout control panel & 2D keyboard map
│       │   ├── ColorPickerWindow.xaml     # Windows 11-style HSV RGB color picker
│       │   └── TrayIcon.cs                # System tray notification icon & live tooltip
│       └── App.xaml                   # Application entry point & resource dictionaries
├── scripts/                           # Build, package & Authenticode signing scripts
│   └── build-and-sign.ps1             # Multi-target build and release packager
├── docs/                              # Project documentation and banner imagery
│   └── cover.png                      # Application showcase banner
├── LICENSE                            # GNU General Public License v3.0
└── E-Helper.slnx                      # Modern Visual Studio solution
```

---

## 🚀 Download & Quick Start

1. Download the latest **[`E-Helper-win-x64.zip`](https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper-win-x64.zip)** package (~64 MB compressed).
2. Extract the archive to your preferred folder (e.g., `C:\Program Files\E-Helper` or your personal user folder).
3. Right-click `E-Helper.exe` and select **Run as Administrator** *(Administrator privileges are required by Windows to access ACPI WMI hardware management interfaces)*.
4. E-Helper will dock into your **System Tray** (near the clock). Click the tray icon to open the flyout control panel!
5. Check **Start with Windows** inside the app to enable seamless background startup with Windows Task Scheduler (no UAC prompts).

> [!TIP]
> **Why download the ZIP package?**
> The standalone compressed ZIP package is **~64 MB**, compared to the ~156 MB uncompressed single-file executable. It downloads faster while containing the complete self-contained application and embedded .NET runtime.

---

## 🛠️ Building from Source

### Prerequisites
* Windows 10 / 11 (64-bit)
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
* Visual Studio 2022 (with *.NET Desktop Development* workload) or VS Code / JetBrains Rider

### Build Steps
```powershell
# Clone the repository
git clone https://github.com/berkevnl/E-Helper.git
cd E-Helper

# Restore dependencies and build release binary
dotnet build src/EHelper/EHelper.csproj -c Release

# Or generate self-contained standalone binaries and release ZIP packages
powershell -ExecutionPolicy Bypass -File scripts/build-and-sign.ps1
```

The compiled standalone executable and release ZIP will be located in the `publish-standalone\` directory.

---

## 💖 Acknowledgments & Philosophy

This project is deeply inspired by **[G-Helper](https://github.com/seerge/g-helper)** created by **[Seerge](https://github.com/seerge)**. E-Helper aims to deliver that same clean, respectful, and ultra-lightweight computing experience directly to Casper Excalibur users.

---

## ⚖️ Trademarks & Legal Disclaimer

* All product names, logos, and brands (including *Casper*, *Excalibur*, *Windows*, and *NVIDIA*) are property of their respective owners and are used strictly for hardware identification and compatibility purposes.
* **E-Helper** is an independent, community-driven open-source project and is not affiliated with, endorsed by, or associated with Casper Bilgisayar Sistemleri A.Ş.
* This software is distributed under the **GNU General Public License v3.0 (GPL-3.0)** in the hope that it will be useful, but WITHOUT ANY WARRANTY of any kind.
