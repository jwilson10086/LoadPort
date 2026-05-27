using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using LoadPort.Models;
using LoadPort.Properties;
using MiniExcelLibs;
using Newtonsoft.Json;
using ZCCommunication.Profinet.Melsec;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Forms.Button;
using ToolTip = System.Windows.Forms.ToolTip;

namespace LoadPort
{
    public partial class Form1 : Form
    {
        #region 字段
        private List<PlcCommandConfig> _commands = new List<PlcCommandConfig>();
        private Dictionary<string, bool> _lastTriggerStates = new Dictionary<string, bool>();
        public static MelsecMcNet plcMcNet;
        public static bool PlcConnected = false;
        private ToolTip toolTip = new ToolTip();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public static bool isConnected { get; set; }

        public class Config
        {
            public PLCConfig PLC { get; set; }
        }

        public class PLCConfig
        {
            public string IP { get; set; }
            public int Port { get; set; }
        }
        #endregion


        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosed += Form1_FormClosed;
        }

        /// <summary>
        /// 窗口关闭前确认
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var result = LoadPort.CustomMessageBox.Show(
                "请注意，关闭会导致软件连接断开!"
            );

            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
            else
            {
                // 通知后台任务退出
                _cts.Cancel();
            }
        }

        /// <summary>
        /// 窗口关闭后释放所有资源
        /// </summary>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                Logger.LogInfo("开始释放资源...");

                // 关闭 LoadPort 串口
                foreach (var lp in LoadPortFactory.AllPorts)
                {
                    if (lp.IsOpen)
                    {
                        try
                        {
                            lp.Close(); // 你 LoadPort 类里应该有 Close 方法封装串口关闭
                            Logger.LogInfo($"LoadPort {lp.Name} 串口已关闭");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"关闭 {lp.Name} 串口失败: {ex.Message}");
                        }
                    }
                }

                // 断开 PLC
                try
                {
                    plcMcNet?.ConnectClose();
                    Logger.LogInfo("PLC 连接已断开");
                }
                catch (Exception ex)
                {
                    Logger.LogError("PLC 断开失败: " + ex.Message);
                }

                // 停止其他后台任务、定时器
                _cts?.Cancel();

                Logger.LogInfo("资源释放完毕");
            }
            catch (Exception ex)
            {
                Logger.LogError("释放资源异常: " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 让主容器垂直堆叠每个 GroupBox，并允许滚动
            flowLayoutPanelMain.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelMain.WrapContents = false; // 自己不换行，垂直堆叠
            flowLayoutPanelMain.AutoScroll = true;

            // 当窗口或容器变宽/变窄时，统一调整每个 GroupBox 和内层 FlowPanel 的宽度
            flowLayoutPanelMain.SizeChanged += (s, ee) =>
            {
                foreach (Control c in flowLayoutPanelMain.Controls)
                {
                    if (c is GroupBox gb)
                        ResizeGroupBoxForParent(gb);
                }
            };

            Logger.AddLogToUI += AddLog;

            // 连接 PLC
            InitPLC();

            StartPlcMonitor();

            // 读取并创建所有 LoadPort
            string lpConfigPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "loadport",
                "loadport_config.json"
            );
            LoadPortFactory.LoadFromConfig(lpConfigPath);
            // 启动 LoadPort 后台任务

            foreach (var lp in LoadPortFactory.AllPorts.Where(p => !p.Bypass))
            {
                if (lp.IsOpen)
                    Logger.LogInfo($"LoadPort {lp.Name} 打开 端口：{lp.PortName}");

                // 启动后台任务
                _ = Task.Run(
                    async () =>
                    {
                        while (!_cts.IsCancellationRequested)
                        {
                            if (isConnected)
                            {
                                await lp.ExecuteCommandsAsync();
                                await Task.Delay(100, _cts.Token).ContinueWith(_ => { });
                            }
                        }
                    },
                    _cts.Token
                );

                // === 动态生成按钮 ===
                CreateButtonsForLoadport(lp);
            }
        }

        private void StartPlcMonitor()
        {
            Task.Run(
                async () =>
                {
                    // 异步初始连接
                    await Task.Run(() => plcMcNet.ConnectServer());

                    while (!_cts.IsCancellationRequested && !this.IsDisposed)
                    {
                        try
                        {
                            // 异步读取PLC状态
                            var result = await Task.Run(() => plcMcNet.ReadBool("M200"));
                            isConnected = result.IsSuccess;

                            // 更新UI（不包含同步操作）
                            this.BeginInvoke(
                                new Action(() =>
                                {
                                    if (isConnected)
                                    {
                                        lblPlccon.Text = "PLC已连接";
                                        lblPlccon.ForeColor = Color.Black;
                                        lblPlccon.BackColor = Color.Green;
                                    }
                                    else
                                    {
                                        lblPlccon.Text = "PLC未连接";
                                        lblPlccon.ForeColor = Color.White;
                                        lblPlccon.BackColor = Color.Red;
                                    }
                                })
                            );

                            // 如果断开连接，在后台线程中重连
                            if (!isConnected)
                            {
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        plcMcNet.ConnectServer();
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.LogError($"PLC重连失败: {ex.Message}");
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // 异常处理也在后台线程
                            Logger.LogError($"PLC监控异常: {ex.Message}");

                            this.BeginInvoke(
                                new Action(() =>
                                {
                                    lblPlccon.Text = "PLC未连接";
                                    lblPlccon.ForeColor = Color.White;
                                    lblPlccon.BackColor = Color.Red;
                                })
                            );

                            // 异步重连
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    plcMcNet.ConnectServer();
                                }
                                catch (Exception reconnectEx)
                                {
                                    Logger.LogError($"PLC异常后重连失败: {reconnectEx.Message}");
                                }
                            });
                        }

                        await Task.Delay(2000, _cts.Token);
                    }
                },
                _cts.Token
            );
        }

        //private void StartPlcMonitor()
        //{
        //    Task.Run(
        //        async () =>
        //        {
        //            isConnected = plcMcNet.ConnectServer().IsSuccess;
        //            while (!_cts.IsCancellationRequested && !this.IsDisposed)
        //            {
        //                try
        //                {
        //                    // 使用异步方式读取PLC状态
        //                    var result = await Task.Run(() => plcMcNet.ReadBool("M200"));
        //                    isConnected = result.IsSuccess;
        //                    this.BeginInvoke(
        //                        new Action(() =>
        //                        {
        //                            if (isConnected)
        //                            {
        //                                lblPlccon.Text = "PLC已连接";
        //                                lblPlccon.ForeColor = Color.Black;
        //                                lblPlccon.BackColor = Color.Green;
        //                            }
        //                            else
        //                            {
        //                                lblPlccon.Text = "PLC未连接";
        //                                lblPlccon.ForeColor = Color.White;
        //                                lblPlccon.BackColor = Color.Red;
        //                                plcMcNet.ConnectServer();
        //                            }
        //                        })
        //                    );
        //                }
        //                catch
        //                {
        //                    this.BeginInvoke(
        //                        new Action(() =>
        //                        {
        //                            lblPlccon.Text = "PLC未连接";
        //                            lblPlccon.ForeColor = Color.White;
        //                            lblPlccon.BackColor = Color.Red;
        //                            plcMcNet.ConnectServer();
        //                        })
        //                    );
        //                }

        //                await Task.Delay(2000, _cts.Token).ContinueWith(_ => { }); // 支持退出
        //            }
        //        },
        //        _cts.Token
        //    );
        //}

        private void InitPLC()
        {
            string configFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "loadport",
                "plc_config.json"
            );
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                Config config = JsonConvert.DeserializeObject<Config>(json);

                plcMcNet = new MelsecMcNet(config.PLC.IP, config.PLC.Port);
                plcMcNet.SetPersistentConnection();
                if (plcMcNet.ConnectServer().IsSuccess)
                {
                    Logger.LogInfo("PLC连接成功");
                    this.lblPlccon.Text = "PLC已连接";
                    this.lblPlccon.ForeColor = Color.Black;
                    this.lblPlccon.BackColor = Color.Green;
                    // PlcConnected = true;
                }
                else
                {
                    //PlcConnected= false;
                    this.lblPlccon.Text = "PLC未连接";
                    this.lblPlccon.ForeColor = Color.White;
                    this.lblPlccon.BackColor = Color.Red;
                    Logger.LogError("PLC连接失败");
                }
            }
            else
            {
                Logger.LogError("PLC配置文件未找到。");
            }
        }

        private void CreateButtonsForLoadport(Loadport lp)
        {
            var groupBox = new GroupBox
            {
                Text = lp.Name,
                AutoSize = false, // 关键：不要 AutoSize
                Height = 140, // 高度可根据按钮多少调整或设为更大
                Margin = new Padding(10, 10, 10, 0) // 与上一组的间距
            };

            // 内层 Flow 面板：用于自动换行按钮
            var flow = new FlowLayoutPanel
            {
                AutoSize = false, // 关键：用固定宽度驱动换行
                WrapContents = true, // 允许换行
                FlowDirection = FlowDirection.LeftToRight,
                Location = new Point(10, 20), // 给 GroupBox 标题留空间
                Height = groupBox.Height - 30, // 简单估算高度
                Margin = new Padding(0)
            };
            groupBox.Controls.Add(flow);

            // 生成按钮
            foreach (var cmd in lp.Commands)
            {
                if (cmd.Command != "" && cmd.Command != null)
                {
                    var btn = new Button
                    {
                        Text = string.IsNullOrWhiteSpace(cmd.Remark) ? cmd.Command : cmd.Remark,
                        Width = 130, // 固定宽度，利于整齐换行
                        Height = 30,
                        Margin = new Padding(3),
                        Tag = new ButtonTag { Lp = lp, Cmd = cmd }
                    };
                    btn.Click += Btn_Click;
                    flow.Controls.Add(btn);
                }
            }

            // 先加入，再按父容器宽度拉伸
            flowLayoutPanelMain.Controls.Add(groupBox);
            ResizeGroupBoxForParent(groupBox); // 初始时按父容器宽度调整
        }

        private void ResizeGroupBoxForParent(GroupBox gb)
        {
            // 计算 groupBox 目标宽度 = 父容器客户区宽度 - 两侧边距
            int targetWidth = flowLayoutPanelMain.ClientSize.Width - gb.Margin.Horizontal - 5;
            int targetHeight = flowLayoutPanelMain.ClientSize.Height / 4; // 保持原高度不变
            if (targetWidth < 100)
                targetWidth = 100; // 最小保护
            gb.Width = targetWidth;

            // 内层 Flow 面板宽度 = groupBox 客区宽度 - 左右内边距
            if (gb.Controls.Count > 0 && gb.Controls[0] is FlowLayoutPanel inner)
            {
                // 预留一点内边距，避免贴边
                inner.Width = gb.ClientSize.Width - inner.Margin.Horizontal - 8;
            }
        }

        public class ButtonTag
        {
            public Loadport Lp { get; set; }
            public PlcCommandConfig Cmd { get; set; }
        }

        private async void Btn_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is ButtonTag tag)
            {
                var lp = tag.Lp;
                var cmd = tag.Cmd;

                // 添加确认对话框
                var result = MessageBox.Show(
                    $"确定要执行命令吗？\n\n设备: {lp.Name}\n命令: {cmd.Command}\n备注: {cmd.Remark}",
                    "确认执行命令",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                // 如果用户选择"否"，则取消操作
                if (result != DialogResult.Yes)
                {
                    Logger.LogInfo($"[{lp.Name}] 用户取消命令执行: {cmd.Command}");
                    return;
                }

                Logger.LogInfo($"[{lp.Name}] 手动发送命令: {cmd.Command}");

                bool ok = await lp.SendWithReplyAsync(
                    cmd.Command,
                    cmd.TimeoutMs,
                    cmd.ExpectedReply.Split('|')
                );
                if (ok)
                    Logger.LogInfo($"[{lp.Name}] 执行成功: {cmd.Remark}");
                else
                    Logger.LogError($"[{lp.Name}] 执行失败或超时: {cmd.Remark}");
            }
        }

        #region 日志写入通用方法

        public void PLCWritelog(string address, int value)
        {
            plcMcNet.Write(address, value);
            Logger.LogInfo($"Write PLC {address} {value}");
        }

        public void PLCWritelog(string address, bool value)
        {
            plcMcNet.Write(address, value);
            Logger.LogInfo($"Write PLC {address} {value}");
        }

        public void PLCWritelog(string address, string value)
        {
            plcMcNet.Write(address, value);
            Logger.LogInfo($"Write PLC {address} {value}");
        }

        private void AddLog(int type, string info)
        {
            if (this.lst_Log.InvokeRequired)
            {
                Action<int, string> action = new Action<int, string>(AddLog);
                lst_Log.Invoke(action, new object[] { type, info });
            }
            else
            {
                ListViewItem lst = new ListViewItem(
                    " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    type
                );
                lst.SubItems.Add(info);
                lst_Log.Items.Insert(0, lst);

                // 调整列宽，保证能显示内容
                lst_Log.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                lst_Log.Columns[lst_Log.Columns.Count - 1].Width = -2; // 最后一列填充
            }
        }

        #endregion
    }
}
