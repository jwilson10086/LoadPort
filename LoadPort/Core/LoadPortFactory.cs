using LoadPort;
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
        if (!File.Exists(configPath))
        {
            Logger.LogError("配置文件不存在: " + configPath);
            return;
        }

        string json = File.ReadAllText(configPath);
        var configs = JsonConvert.DeserializeObject<List<LoadPortConfig>>(json);

        foreach (var cfg in configs)
        {
            if (cfg.Bypass) continue; // 屏蔽的跳过

            var port = new Loadport(cfg.Name, cfg.PortName, cfg.BaudRate, cfg.DataBits,
                                    (StopBits)cfg.StopBits,
                                    (Parity)cfg.Parity);

            // 加载 CSV 命令
            string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "loadport", cfg.Name + ".csv");
            if (!File.Exists(csvPath))
            {
                //MessageBox.Show($"{cfg.Name}.csv 不存在，请手动添加对应 CSV 文件", "文件缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Logger.LogError($"{cfg.Name}.csv 不存在,请手动添加对应 CSV 文件");
            }
            else
            {
                port.Commands = CsvHelper.LoadCsvCommands(csvPath);
                if(port.Commands != null)
                {
                    Logger.LogInfo($"加载 {cfg.Name} 的命令: {port.Commands.Count} 条");
                }
                else
                {
                    Logger.LogError($"加载 {cfg.Name} 的命令失败,请检查 CSV 文件格式");
                    return;
                }
            }

            AllPorts.Add(port);
        }
    }

    public static Loadport GetPortByName(string name)
    {
        return AllPorts.FirstOrDefault(p => p.Name == name);
    }
}

