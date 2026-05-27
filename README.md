# Oscilloscope Test Demo

> Keysight InfiniiVision 示波器上位机采集工具 —— 基于 SCPI / TCP 协议的波形数据采集与电流监控

[![Platform](https://img.shields.io/badge/Platform-Windows-blue)](https://github.com)
[![Framework](https://img.shields.io/badge/.NET_Framework-4.7.2-purple)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23-green)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Protocol](https://img.shields.io/badge/Protocol-SCPI%20%2F%20TCP-orange)](https://www.keysight.com)

---

## 项目简介

本工具为 Keysight InfiniiVision HD3 系列示波器上位机采集软件，通过 TCP Socket 连接示波器，发送 SCPI 指令进行波形数据采集，并将采集到的电流曲线数据保存为 CSV 文件，用于后续分析。

项目来源于电镀机设备研发中的**电流监控**需求，支持两种数据格式：ASCII 文本模式与 Binary 二进制高速模式。

---

## 功能特性

- **双采集模式**：
  - **ASCII 模式**：逐点文本数据，便于调试和快速验证
  - **Binary 模式**：16-bit 二进制波形块（IEEE 488.2 `#N` 格式），采集速度更快，适合长时间高密度采集
- **TCP/IP 连接**：通过仪器 LAN 接口（端口 5025）连接，无需 GPIB/USB 适配器
- **SCPI 命令交互**：完整的发送/接收日志，时间戳精确到毫秒
- **连续采集**：可设置采集时长（秒）和采样点数，支持循环采集直到手动停止
- **数据持久化**：每次采集结果自动保存为带时间戳的 CSV 文件，路径 `D:\OscilloscopeData\`
- **通道配置**：可选通道（CHAN1~CHAN4）及探头倍率（1x / 10x）
- **实时日志**：界面实时显示 SEND/RECV/ERROR 三类日志，带毫秒时间戳

---

## 技术栈

| 组件 | 版本 | 说明 |
|------|------|------|
| .NET Framework | 4.7.2 | 运行时框架 |
| WinForms | — | 桌面 UI |
| System.Net.Sockets | — | TCP 通信 |
| System.Text.Json | 10.0.8 | 数据处理 |
| Ivi.Visa | 8.0 | VISA 仪器通信（备选） |

---

## 项目结构

```
OscilloscopeTestDemo/
├── Ascii.cs              # ASCII 模式采集窗口（文本数据，逐点输出）
├── Ascii.Designer.cs     # ASCII 窗口 UI 设计器代码
├── Binary.cs             # Binary 模式采集窗口（IEEE 488.2 二进制块）
├── Binary.Designer.cs    # Binary 窗口 UI 设计器代码
├── Program.cs            # 程序入口
├── Properties/           # 程序属性和资源
├── packages.config       # NuGet 包配置
└── OscilloscopeTestDemo.sln
```

---

## 快速开始

### 环境要求

- Windows 10 / Windows 11
- Visual Studio 2019 / 2022
- .NET Framework 4.7.2
- Keysight InfiniiVision 系列示波器（支持 LAN + SCPI，端口 5025）
- 示波器与电脑在同一局域网内

### 编译运行

1. 克隆仓库：
   ```bash
   git clone <repo-url>
   cd OscilloscopeTestDemo
   ```

2. 用 Visual Studio 打开 `OscilloscopeTestDemo.sln`

3. 还原 NuGet 包（右键解决方案 → 还原 NuGet 包）

4. 编译并运行（F5）

### 使用方法

1. 在界面输入示波器 IP 地址（如 `192.168.1.5`）
2. 选择目标通道（CHAN1 ~ CHAN4）和探头倍率
3. 设置采集时长和采样点数
4. 点击 **Connect** 连接示波器
5. 点击 **Start** 开始采集，采集结果自动保存到 `D:\OscilloscopeData\`
6. 点击 **Stop** 停止采集

### 数据输出

采集结果保存为 CSV 文件：

```
D:\OscilloscopeData\scope_20260527_134500.csv
```

| 列名 | 说明 |
|------|------|
| Time | 采集时间戳 |
| Current | 电流值（A） |

---

## SCPI 命令说明

| 命令 | 说明 |
|------|------|
| `*IDN?` | 查询仪器标识 |
| `:WAVeform:SOURce CHAN1` | 设置波形源通道 |
| `:WAVeform:FORMat ASCii` | 设置 ASCII 格式 |
| `:WAVeform:FORMat WORD` | 设置 Binary 16-bit 格式 |
| `:WAVeform:DATA?` | 请求波形数据 |
| `:WAVeform:PREamble?` | 查询波形前导（时间/电压缩放参数）|
| `:DIGitize CHAN1` | 触发单次采集 |

---

## 适用设备

- Keysight InfiniiVision HD3 系列（DSOX / MSOX）
- 其他支持 SCPI over TCP/5025 的 Keysight 示波器

---

## 注意事项

- 确保示波器 LAN 已启用，可在示波器菜单 **Utility → I/O → LAN** 中确认 IP
- 采集数据保存路径 `D:\OscilloscopeData\` 需提前创建，或修改源码中的 `SaveDirectory` 常量
- Binary 模式采集速度更快，推荐用于长时间连续监控场景

---

## License

本项目为内部研发工具，仅供内部使用。
