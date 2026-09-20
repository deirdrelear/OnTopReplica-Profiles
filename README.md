# OnTopReplica Profiles

This fork extends OnTopReplica Refactor with read-only multi-client profiles and automatic replica layouts.

Development branch: `profiles-v1`.

## Profile manager

Run:

```
OnTopReplica.exe --profiles
```

Profiles are read from:

```
%APPDATA%\OnTopReplica\Profiles
```

A profile maps source windows to roles and persistent slots. Each role can create zero, one or several DWM replicas. This supports mixed layouts such as 18 mining barges, one booster with no replica, and two scouts with a different layout.

Managed replicas are view-only. The profile build removes the command-line click-forwarding option, hides the legacy click-forwarding UI, and replaces the old mouse-injection implementation with a no-op compatibility stub.

See `examples/Mining.example.json`, `examples/Rampant.example.json`, and `CHANGELOG_PROFILES.md`.

---

> **⚠️ OnTopReplica is archived and will no longer be updated. Check out the new app [OnTop-Windows](https://github.com/giahoki/OnTop-Windows) — rewritten in C++ from scratch!**

# OnTopReplica Refactor

**A real-time always-on-top "replica" of a window of your choice, for Windows Vista, 7, 8, 10, and 11.**

This simple utility application shows a blank always-on-top window by default. Users can pick any other window of the system to have an always up-to-date clone of the target window shown always-on-top. Very useful for monitoring background processes, wrangling with complex multi-window games or tools, watching YouTube videos while working, and so on.

This is an enhanced fork of the original [OnTopReplica by LorenzCK](https://github.com/LorenzCK/OnTopReplica), featuring .NET 4.8 migration, Windows 11 support, hotkeys, language selection, enhanced fullscreen modes, and more.

> 🇷🇺 [Русская версия](README_RU.md)

---

## 📢 Features

### Core Functionality
- **Clone any window** and keep it *always-on-top* while working with other windows
- **Select a subregion** of the cloned window:
  - Can be stored for future use
  - Can use relative coordinates from the target window's borders
- **Auto-resizing** — fit the original window, half, quarter, and fullscreen modes
- **Position lock** on any corner of your screen
- **Adjustable opacity** for transparent overlays

### Interaction
- **"Click forwarding"** — interact with the cloned window directly
- **"Click-through"** — makes the replica ignore any mouse interaction (turns OnTopReplica into an overlay when combined with partial opacity)
- **"Group switch" mode** — automatically switches through a group of windows while you use them

### New in Refactor ✨
- **⌨️ Global Hotkeys**
  - Configurable hotkey to show/hide OnTopReplica
  - Configurable hotkey to clone the currently focused window
  - Manage hotkeys in the Options panel

- **🌍 Multi-Language Support**
  - Choose from 12+ interface languages with flag icons
  - English, Čeština, Dansk, Deutsch, Español, Italiano, Polski, Русский, 简体中文, 繁體中文, Português, 日本語
  - Auto-detects system language by default

- **🖥️ Enhanced Fullscreen Modes**
  - **Standard** — fills the working area (excludes taskbar)
  - **Fullscreen** — covers the entire screen including taskbar
  - **All Screens** — spans across all connected monitors

- **🔧 Technical Improvements**
  - Migrated to **.NET Framework 4.8**
  - Updated to latest **WindowsFormsAero** library
  - **Windows 11** support
  - Removed auto-update system for cleaner, more stable builds
  - Improved form positioning and sizing (no white borders)
  - MSI installer for easy deployment

---

## 📸 Screenshots

> *Add screenshots here*

---

## Requirements

- Microsoft Windows Vista or greater (the application makes use of native DWM Thumbnails to create replicas)
- Microsoft .NET Framework 4.8
- Desktop Composition (a.k.a. Windows *Aero*) enabled

---

## Installation

Get the [latest version](https://github.com/vintik/OnTopReplica-Refactor/releases) from the releases section as an MSI installer.

### Build from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/vintik/OnTopReplica-Refactor.git
   ```

2. Open `src/OnTopReplica.sln` in Visual Studio 2019 or later

3. Restore NuGet packages and build the solution

---

## Usage

### Basic
1. Launch OnTopReplica — a blank always-on-top window appears
2. Right-click to open the context menu
3. Select **Select Window** and click on the window you want to clone
4. The replica appears in the OnTopReplica window

### Advanced
- **Select Region** — right-click and choose to select a specific area of the cloned window
- **Fullscreen** — switch between fullscreen modes for immersive viewing
- **Opacity** — adjust transparency for overlay use cases
- **Hotkeys** — configure global shortcuts in Options for quick access
- **Language** — change the interface language in Options

---

## Contributions

…are very welcome. Fork away! 🍽️

Submitting [issues](https://github.com/vintik/OnTopReplica-Refactor/issues) and other feedback is also appreciated.

### Roadmap

- [x] Update to the newest [WindowsFormsAero](https://github.com/LorenzCK/WindowsFormsAero) version
- [x] Migrate to .NET 4.8
- [x] Remove auto-update system
- [x] Add global hotkey support
- [x] Add multi-language selection
- [x] Add enhanced fullscreen modes
- [ ] Improve/add **High DPI** support
- [ ] "Stored scenarios" that, just like stored regions, automatically clone a window (based on title or window class criteria), select a region, and set other options. Ideally to be used as Taskbar shortlinks

---

## License

**OnTopReplica Refactor** is licensed under the [MS-RL (Microsoft Reciprocal License)](https://github.com/vintik/OnTopReplica-Refactor/blob/master/LICENSE).

Original OnTopReplica by [LorenzCK](https://github.com/LorenzCK/OnTopReplica).

---

## Credits

- Original author: [LorenzCK](https://github.com/LorenzCK)
- WindowsFormsAero library: [LorenzCK](https://github.com/LorenzCK/WindowsFormsAero)
- Contributors and translators from the community

See [CREDITS.txt](src/OnTopReplica/CREDITS.txt) for full details.
