# OrbitWheel

OrbitWheel 是一款 Windows 径向快捷操作工具。当前发布版本为 **OrbitWheel-Lite Fin**，这是 Lite 系列的最终版本。

按住自定义快捷键后，鼠标位置会出现六等分圆环。将鼠标移向目标扇区并松开快捷键，即可执行对应操作。

## Lite 版功能

- 默认快捷键：`Ctrl + Space`
- 默认使用“按住并松开执行”模式
- 六扇区径向菜单，圆心固定在唤出时的鼠标位置
- 鼠标滚轮切换页面，页面数量不限
- 支持打开程序、执行命令、资源管理器、锁定、睡眠、关机、重启和音量控制
- 支持液态玻璃、高斯模糊、亚克力三种视觉材质
- 设置修改后自动保存
- 支持随 Windows 自动启动
- 关闭窗口后常驻系统托盘

## 使用

1. 下载 Release 中的 `OrbitWheel-Lite-Fin.zip`。
2. 解压后运行 `OrbitWheel-Lite.exe`。
3. 双击托盘图标打开设置。

配置保存在 `%APPDATA%\OrbitWheel\config.json`。

## 构建

系统要求：Windows PowerShell 5.1 和 .NET Framework 4.x。

```powershell
.\build.ps1
```

构建结果位于 `dist\OrbitWheel-Lite.exe`。

## Lite 版说明

Lite 版使用单文件 WinForms 实现，无需额外安装运行库。`Fin` 为 Lite 系列最终版本标识。

## 许可证

[MIT License](LICENSE)
