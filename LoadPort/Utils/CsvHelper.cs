using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoadPort.Models;

namespace LoadPort
{
    public static class CsvHelper
    {
        public static List<PlcCommandConfig> LoadCsvCommands(string csvPath)
        {
            var list = new List<PlcCommandConfig>();

            if (!File.Exists(csvPath))
            {
                Logger.LogError($"CSV 文件不存在: {csvPath}");
                return list;
            }
            try
            {
                var lines = File.ReadAllLines(csvPath, Encoding.GetEncoding("GB2312")); // 指定编码，防止中文乱码

                for (int i = 1; i < lines.Length; i++) // 从 1 开始，跳过标题行
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue; // 支持注释行

                    var cols = line.Split(',');

                    if (cols.Length < 7)
                    {
                        Logger.LogError($"CSV 格式错误，至少7列: {line}");
                        continue;
                    }

                    var cfg = new PlcCommandConfig
                    {
                        Command = cols[0].Trim(),
                        ExpectedReply = cols[1].Trim(),
                        TimeoutMs = int.TryParse(cols[2].Trim(), out int t) ? t : 3000,
                        TriggerBit = cols[3].Trim(),
                        ReturnAddress = string.IsNullOrWhiteSpace(cols[4].Trim())
                            ? null
                            : cols[4].Trim(),
                        //ReturnAddress = string.IsNullOrWhiteSpace(cols[4].Trim())
                        //    ? null
                        //    : cols[4].Trim().Split('|').ToList(),

                        ReturnBit = string.IsNullOrWhiteSpace(cols[5].Trim())
                            ? null
                            : cols[5].Trim(),
                        ReturnBitFail = cols[6].Trim(),
                        Remark =
                            cols.Length > 7 && !string.IsNullOrWhiteSpace(cols[7].Trim())
                                ? cols[7].Trim()
                                : null,
                        //Remark = cols.Length > 8 && !string.IsNullOrWhiteSpace(cols[8].Trim()) ? cols[8].Trim() : null,
                    };

                    list.Add(cfg);
                }

                Logger.LogInfo($"成功加载 CSV 命令：{Path.GetFileName(csvPath)}, 共 {list.Count} 条命令");
                return list;
            }
            catch (Exception ex )
            {
                Logger.LogError($"程序运行报错：{ex}");
                return null;
                //throw;
            }
        }
    }
}
