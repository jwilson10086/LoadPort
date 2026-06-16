using LoadPort;
using LoadPort.Comm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

public static class LoadPortFactory
{
    public static List<Loadport> AllPorts { get; private set; } = new List<Loadport>();

    public static void LoadFromConfig(string configPath)
    {
        AllPorts.Clear();

        if (!File.Exists(configPath))
        {
            Logger.LogError("LoadPort 配置文件不存在: " + configPath);
            return;
        }

        string json = File.ReadAllText(configPath);
        var configs = JsonConvert.DeserializeObject<List<LoadPortConfig>>(json);

        foreach (var cfg in configs)
        {
            if (cfg.Bypass) continue;

            // ── 根据 CommMode 创建对应通道 ──────────────────────────
            ICommChannel channel;
            if (cfg.CommMode == CommMode.Tcp)
            {
                channel = new TcpCommChannel(cfg.Name, cfg.TcpIp, cfg.TcpPort);
                Logger.LogInfo($"[{cfg.Name}] 使用 TCP 通道 {cfg.TcpIp}:{cfg.TcpPort}");
            }
            else
            {
                channel = new SerialCommChannel(
                    cfg.Name,
                    cfg.PortName,
                    cfg.BaudRate,
                    cfg.DataBits,
                    (StopBits)cfg.StopBits,
                    (Parity)cfg.Parity
                );
                Logger.LogInfo($"[{cfg.Name}] 使用 串口 通道 {cfg.PortName} @ {cfg.BaudRate}bps");
            }

            var port = new Loadport(cfg.Name, channel)
            {
                LoadPortConfig = cfg
            };

            // ── 加载 CSV 命令表 ────────────────────────────────────
            string csvPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "loadport", cfg.Name + ".csv");

            if (!File.Exists(csvPath))
            {
                Logger.LogError($"{cfg.Name}.csv 不存在，请手动添加对应 CSV 文件");
            }
            else
            {
                port.Commands = CsvHelper.LoadCsvCommands(csvPath);
                if (port.Commands != null)
                    Logger.LogInfo($"加载 {cfg.Name} 命令：{port.Commands.Count} 条");
                else
                    Logger.LogError($"加载 {cfg.Name} 命令失败，请检查 CSV 文件格式");
            }

            AllPorts.Add(port);
        }
    }

    public static Loadport GetPortByName(string name)
        => AllPorts.FirstOrDefault(p => p.Name == name);
}

