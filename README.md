# SMIF LoadPort Controller

> 赛谨 SMIF LoadPort 上位机控制软件 —— 基于串口协议的晶圆盒传输接口控制系统

[![Platform](https://img.shields.io/badge/Platform-Windows-blue)](https://github.com)
[![Framework](https://img.shields.io/badge/.NET_Framework-4.7.2-purple)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-C%23-green)](https://docs.microsoft.com/en-us/dotnet/csharp/)

---

## 项目简介

本项目为半导体设备 SMIF（Standard Mechanical Interface）LoadPort 上位机控制软件，通过串口通信与 LoadPort 硬件进行交互，同时与三菱 PLC（MC 协议）联动，实现晶圆盒的自动化传输控制与监控。

主要用于电镀机、清洗机等半导体设备中 LoadPort 接口的上位机调试与集成测试。

---

## 功能特性

- **串口通信**：可配置波特率、数据位、停止位、校验位，连接 LoadPort 硬件
- **PLC 联动**：通过三菱 MC 协议（`ZCCommunication`）与 PLC 通信，接收 LoadPort 动作反馈并写入 PLC 位元
- **命令配置化**：命令及预期响应通过 JSON/XML 配置文件定义，无需修改代码即可调整控制流程
- **异步消息处理**：基于 `TaskCompletionSource` 的异步等待机制，支持超时和多响应匹配
- **实时日志**：操作日志实时写入文件，方便设备调试与故障追溯
- **Excel 测试记录**：集成 MiniExcel，将测试结果导出为 Excel 文件
- **Bypass 模式**：支持不连接硬件的旁路调试模式

---

## 技术栈

| 组件 | 版本 | 说明 |
|------|------|------|
| .NET Framework | 4.7.2 | 运行时框架 |
| WinForms | — | 桌面 UI |
| ZCCommunication | 1.0.0 | 三菱 MC 协议 PLC 通信库 |
| Newtonsoft.Json | 13.0.3 | JSON 配置解析 |
| MiniExcel | 1.28.5 | Excel 报表导出 |

---

## 项目结构

```
赛谨Smif/
├── LoadPort/
│   ├── Core/
│   │   ├── LoadPort.cs           # LoadPort 核心控制类（串口收发、命令匹配）
│   │   ├── LoadPortFactory.cs    # LoadPort 实例工厂
│   │   └── SendMessage.cs        # 发送消息封装
│   ├── Models/
│   │   ├── LoadPortConfig.cs     # LoadPort 配置模型
│   │   └── PlcCommandConfig.cs   # PLC 命令配置模型
│   ├── Utils/
│   │   ├── Logger.cs             # 日志工具
│   │   └── CsvHelper.cs          # CSV/Excel 辅助工具
│   ├── Form1.cs                  # 主界面（设备连接、命令发送、状态监控）
│   ├── SerialManager.cs          # 串口管理基类
│   ├── Config.cs                 # 串口配置模型
│   └── CustomMessageBox.cs       # 自定义对话框
└── LoadPort.sln
```

---

## 快速开始

### 环境要求

- Windows 10 / Windows 11
- Visual Studio 2019 / 2022
- .NET Framework 4.7.2
- 三菱 PLC（iQ-R / Q 系列，支持 MC 协议）
- SMIF LoadPort 硬件（RS-232/RS-485 串口连接）

### 编译运行

1. 克隆仓库：
   ```bash
   git clone <repo-url>
   cd 赛谨Smif
   ```

2. 用 Visual Studio 打开 `LoadPort.sln`

3. 还原 NuGet 包（右键解决方案 → 还原 NuGet 包）

4. 修改配置文件中的 PLC IP 地址和串口参数

5. 编译并运行（F5）

### 配置说明

程序启动时读取同目录下的 `config.json`，配置项说明：

```json
{
  "PLC": {
    "IP": "192.168.1.100",
    "Port": 502
  }
}
```

LoadPort 串口参数及命令映射在界面上动态配置，并支持导入 JSON 配置文件。

---

## 通信协议

- **LoadPort ↔ 上位机**：RS-232/RS-485 串口，SEMI E84 / E87 标准命令集
- **上位机 ↔ PLC**：三菱 MC 协议（TCP/IP），写入位元反馈 LoadPort 动作状态

---

## 注意事项

- 串口连接前请确认 LoadPort 硬件已上电并完成初始化
- PLC IP 和端口需与实际网络配置一致
- 关闭程序前会弹出确认对话框，防止意外断开连接

---

## License

本项目为内部工具，仅供内部使用。
