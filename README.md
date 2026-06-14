# OrbitWheel

OrbitWheel 是一款 Windows 径向快捷操作工具。按下自定义快捷键后，鼠标位置会出现六等分圆环，将鼠标移向目标扇区即可快速执行操作。

## OrbitWheel 1.0

OrbitWheel 1.0 采用全新设计的深色玻璃 UI 与整套系统操作图标：

![OrbitWheel 1.0 全新 UI](design/orbitwheel-1.0-ui.png)

- 全新设置界面：左侧导航、玻璃卡片和分区设置页面
- 全新纯图标径向菜单，环内不显示动作名称
- 重新设计睡眠、音量、锁定、关机、重启等系统操作图标
- Applications 应用选择器，可识别 Store/UWP 和桌面应用图标
- 支持直接浏览普通程序文件
- 已运行的桌面程序优先切换至现有窗口
- 快捷键支持直接录制任意组合键
- 默认使用“按住并松开执行”模式
- 鼠标滚轮切换页面，支持无限页面
- 设置修改后自动保存
- 支持随 Windows 启动并常驻系统托盘

### 全新系统图标设计

![OrbitWheel 1.0 系统图标](assets/system-icons-sheet.png)

## 使用

1. 下载 Release 中的 `OrbitWheel-1.0.zip`。
2. 解压后运行 `OrbitWheel.exe`。
3. 双击托盘图标打开设置。

配置保存在 `%APPDATA%\OrbitWheel\config.json`。

## 构建

系统要求：Windows PowerShell 5.1 和 .NET Framework 4.x。

```powershell
.\build.ps1
```

构建结果位于 `dist\OrbitWheel.exe`。

## 许可证

[MIT License](LICENSE)
