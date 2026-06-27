<div align="center">

# 🌀 OrbitWheel

### 鼠标原地召出的 Windows 径向快捷操作工具

**Windows Desktop · Radial Launcher · Tray App · System Actions · Fast Workflow**

<p>
  <strong>语言</strong><br/>
  <strong>简体中文</strong> ·
  <a href="#english">English</a>
</p>

<p>
  <strong>导航</strong><br/>
  <a href="#项目简介">项目简介</a> ·
  <a href="#主要功能">主要功能</a> ·
  <a href="#使用方法">使用方法</a> ·
  <a href="#构建">构建</a> ·
  <a href="#许可证">许可证</a>
</p>

![Platform](https://img.shields.io/badge/Platform-Windows%20Desktop-111827)
![UI](https://img.shields.io/badge/UI-Radial%20Wheel-0F766E)
![Actions](https://img.shields.io/badge/Actions-Apps%20%2F%20Folders%20%2F%20System-1D4ED8)
![Build](https://img.shields.io/badge/Build-PowerShell%205.1-7C3AED)
![License](https://img.shields.io/badge/License-MIT-FF5722)
![Release](https://img.shields.io/github/v/release/V0idream/OrbitWheel?include_prereleases)
![Downloads](https://img.shields.io/github/downloads/V0idream/OrbitWheel/total)

</div>

---

<a id="中文"></a>

<a id="项目简介"></a>

## 📌 项目简介

**OrbitWheel** 是一款 Windows 径向快捷操作工具。按下自定义快捷键后，鼠标位置会弹出六等分圆环；将鼠标移向目标扇区并松开，即可快速启动应用、打开文件夹或执行常用系统操作。

它适合需要频繁切换软件、打开常用位置、执行电源 / 音量 / 锁屏等操作的 Windows 用户。相比传统启动器，OrbitWheel 更强调“鼠标原地召出、方向选择、快速执行”的操作体验。

## 🖼️ 界面预览

![OrbitWheel 1.0 全新 UI](design/orbitwheel-1.0-ui.png)

<a id="主要功能"></a>

## ✨ 主要功能

* 六等分径向菜单，扇区从右上开始顺时针编号 `1–6`。
* 支持按住快捷键并松开执行，也支持数字键直接执行对应扇区。
* 鼠标滚轮或左右方向键切换页面，支持无限页面。
* 支持启动桌面程序、Store / UWP 应用和普通程序文件。
* 支持把文件夹设为动作目标，并通过资源管理器打开。
* 打开软件前检测运行状态；目标已运行时优先切换到现有窗口。
* 对关闭到系统托盘的软件提供唤醒兼容逻辑，减少重复启动。
* 全新设置界面：左侧导航、玻璃卡片和分区配置页面。
* 全新纯图标径向菜单，环内不显示动作名称。
* 重新设计睡眠、音量、锁定、关机、重启等系统操作图标。
* 鼠标靠近屏幕边缘时，圆环会自动移动到可完整显示的位置。
* 设置修改后自动保存。
* 支持随 Windows 启动并常驻系统托盘。

### 系统图标设计

![OrbitWheel 1.0 系统图标](assets/system-icons-sheet.png)

<a id="使用方法"></a>

## 🚀 使用方法

1. 下载 `OrbitWheel-1.1.2.zip`。
2. 解压后运行 `OrbitWheel.exe`。
3. 双击托盘图标打开设置。
4. 录制快捷键，配置每个扇区的动作。

配置文件保存在：

```text
%APPDATA%\OrbitWheel\config.json
```

<a id="构建"></a>

## 🛠️ 构建

系统要求：Windows PowerShell 5.1 和 .NET Framework 4.x。

```powershell
.\build.ps1
```

构建结果位于：

```text
dist\OrbitWheel.exe
```

<a id="许可证"></a>

## 📜 许可证

[MIT License](LICENSE)

---

<a id="english"></a>

## English

**OrbitWheel** is a radial shortcut launcher for Windows. Press a custom hotkey and a six-section command wheel appears at the mouse position; move toward a sector and release to launch an app, open a folder, or run a common system action.

It is designed for Windows users who frequently switch apps, open repeated locations, or trigger power, volume, lock, and other system actions. Instead of a traditional launcher window, OrbitWheel focuses on an in-place, direction-based workflow.

## Preview

![OrbitWheel 1.0 New UI](design/orbitwheel-1.0-ui.png)

## Features

* Six-section radial menu, numbered clockwise from the upper-right sector as `1–6`.
* Supports hold-and-release execution and direct sector execution with number keys.
* Switch pages with the mouse wheel or left/right arrow keys; unlimited pages are supported.
* Launch desktop programs, Store / UWP apps, and regular executable files.
* Use folders as action targets and open them with File Explorer.
* Detects whether a target app is already running and switches to the existing window when possible.
* Provides tray wake-up compatibility for apps minimized to the system tray, reducing duplicate launches.
* Redesigned settings window with side navigation, glass cards, and section-based configuration pages.
* Icon-only radial menu without action names inside the wheel.
* Redesigned icons for sleep, volume, lock, shutdown, restart, and other system actions.
* Automatically repositions the wheel near screen edges so the full menu remains visible.
* Saves settings automatically after changes.
* Supports Windows startup and persistent system tray operation.

### System Icon Design

![OrbitWheel 1.0 System Icons](assets/system-icons-sheet.png)

## Usage

1. Download `OrbitWheel-1.1.2.zip`.
2. Extract it and run `OrbitWheel.exe`.
3. Double-click the tray icon to open settings.
4. Record a hotkey and configure actions for each sector.

Configuration is saved at:

```text
%APPDATA%\OrbitWheel\config.json
```

## Build

Requirements: Windows PowerShell 5.1 and .NET Framework 4.x.

```powershell
.\build.ps1
```

The build output is located at:

```text
dist\OrbitWheel.exe
```

## License

[MIT License](LICENSE)
