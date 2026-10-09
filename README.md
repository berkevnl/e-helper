<p align="center">
  <img src="docs/cover.png" alt="E-Helper — Casper Excalibur Gaming Laptop Control Center" width="100%">
</p>

# E-Helper — Casper Excalibur Control Center

<p align="center">
  <a href="https://github.com/berkevnl/E-Helper/releases"><img src="https://img.shields.io/badge/Version-v1.0.1-0078D4?style=flat-square" alt="Version v1.0.1"></a>
  <a href="https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper.exe"><img src="https://img.shields.io/badge/Download-E--Helper.exe-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Direct Download"></a>
  <a href="https://github.com/berkevnl/E-Helper/blob/main/LICENSE"><img src="https://img.shields.io/badge/License-GPL--3.0-green.svg?style=flat-square" alt="License"></a>
  <a href="#-why-e-helper-advantages"><img src="https://img.shields.io/badge/RAM-5--10_MB-success?style=flat-square" alt="RAM Usage"></a>
  <a href="README.tr.md"><img src="https://img.shields.io/badge/Dil-T%C3%BCrk%C3%A7e-red?style=flat-square" alt="Türkçe Döküman"></a>
</p>

---

**E-Helper** is an ultra-lightweight, dedicated open-source hardware control center engineered specifically for **Casper Excalibur** gaming laptops. It is designed to replace heavy, unstable, and bloated OEM vendor software with a single, respectful, zero-bloat portable executable.

Modeled after the beloved **G-Helper** philosophy, E-Helper directly interfaces with Casper Excalibur ACPI/EC firmware (via WMI/SMI and NVML), presenting a unified, lightning-fast flyout control panel.

<p align="center">
  <a href="https://github.com/berkevnl/E-Helper/releases/latest/download/E-Helper.exe">
    <img src="https://img.shields.io/badge/DOWNLOAD_LATEST_RELEASE-E--Helper.exe-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="Download E-Helper.exe" height="46">
  </a>
  <br>
  <sub><i>If the direct download button does not start automatically, please visit <a href="https://github.com/berkevnl/E-Helper/releases/latest">Releases</a> to download the latest executable.</i></sub>
</p>

---

## ⚡ Why E-Helper? (Advantages)

1. **Dedicated to Casper Excalibur:** Tailored specifically for Casper Excalibur gaming laptop motherboards and embedded controllers.
2. **Ultra-Low Memory Footprint:** Consumes only **~5 - 10 MB RAM** idling in the system tray and **~20 - 35 MB RAM** when the flyout is actively open.
3. **Zero Background Bloatware:** No background daemons, no telemetry trackers, no analytics agents, and no unnecessary services eating CPU cycles.
4. **Permanent Settings Persistence:** User configurations (RGB keyboard lighting, power profile, brightness) automatically survive system restarts and shutdowns via elevated Windows Task Scheduler startup and delayed EC post-boot synchronization.
5. **Direct Hardware Communication:** Talks directly to the motherboard Embedded Controller (EC / ACPI SMI / WMI), completely bypassing bloated OEM background processes.
6. **Instant Flyout UI:** Lives discreetly in your System Tray (Taskbar Notification Area). Clicking the tray icon snaps open an elegant, high-DPI flyout panel that can also be freely dragged anywhere on screen.
7. **Bilingual & Dual Theme:** Instant toggles between Turkish and English, plus authentic Matte Dark and Clean Light themes.

---

## 🎯 Features

### 1. Performance Profiles
* **Eco (Office):** Low fan RPM, quiet operation, and energy efficiency.
* **Balanced (Gaming):** Dynamic fan curves and balanced thermals for everyday gaming.
* **Performance (Turbo):** Maximum fan airflow, maximum thermal headroom, and peak sustained clock speeds.
* *Integrated with Windows native power plans (`PowerSetActiveScheme`).*

### 2. Real-Time Telemetry
* **CPU & GPU Metrics:** Real-time temperature readouts (°C) and tachometer fan speeds (RPM).
* **System Resources:** Live RAM allocation (used / total GB and percentage) and primary SSD (C:) storage utilization.
* **System Tray Tooltip:** Hovering over the tray icon reveals current temperatures and fan RPMs without opening the window.

### 3. RGB Keyboard Backlight Control
* **Modes:** Static, Breathing, Colorful Dynamic Cycle, and Rainbow.
* **Interactive Color Spectrum:** Smooth 360° hue selector with curated quick-access preset palettes.
* **Hardware Brightness Steps:**
  * `0` : **Off (0%)** — Shuts down all keyboard LED zones.
  * `1` : **50% Brightness** — Balanced ambient illumination.
  * `2` : **100% Brightness** — Full maximum brightness.
* **Reboot Resilience:** Preserves your lighting mode, brightness, and colors even after cold boots.

### 4. Usability & System Integration
* **Start with Windows:** Elevated logon task scheduling that silently starts E-Helper minimized to tray without triggering UAC warnings.
* **One-Click Theme Switch:** Toggle between Authentic Matte Dark and Clean High-Contrast Light mode.
* **Integrated Auto-Updater:** Checks GitHub Releases for new updates with a single click.

---

## 💻 Hardware Compatibility

E-Helper is exclusively designed for the **Casper Excalibur** family:

### ✅ Casper Excalibur Family (Native ACPI SMI Engine)
> Verified on physical hardware: **Excalibur G770**, **G850**, **G870**, and **G911** have been tested and work flawlessly (power profiles, telemetry, and keyboard RGB lighting).

* **Excalibur G770** (Tested & Verified)
* **Excalibur G850 / G860** (Tested & Verified)
* **Excalibur G870** (Tested & Verified)
* **Excalibur G911** (Tested & Verified)
* **Excalibur G780**
* **Excalibur G900**
* **Excalibur G920**

---

## 🚀 Download & Quick Start

1. Go to the [Releases](https://github.com/berkevnl/E-Helper/releases/latest) page.
2. Download `E-Helper.exe` (standalone portable binary, no installation required).
3. Right-click `E-Helper.exe` and select **Run as Administrator** *(Administrator privileges are required by Windows to communicate with ACPI WMI SMI hardware ports)*.
4. E-Helper docks into your **System Tray** (near the clock). Click the E-Helper icon to open your control flyout!

---

## 💖 Acknowledgments & Inspiration

This project draws inspiration from the **[G-Helper](https://github.com/seerge/g-helper)** project by **[Seerge](https://github.com/seerge)**. E-Helper brings that clean, lightweight philosophy directly to Casper Excalibur users.

---

## ⚖️ Trademarks & Legal Disclaimer

* All product names, logos, and brands mentioned in this project are property of their respective owners and are used solely for identification and hardware compatibility purposes.
* **E-Helper** is an independent, community-driven open-source project.
* This software is distributed under the **GNU General Public License v3.0 (GPL-3.0)** "as is", without warranty of any kind.
