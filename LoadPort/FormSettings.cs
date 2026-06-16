using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using LoadPort.Models;
using Newtonsoft.Json;

namespace LoadPort
{
    /// <summary>
    /// 通讯设置窗口
    /// 支持每个 LoadPort 独立切换 串口 / TCP 模式，并保存到 loadport_config.json
    /// </summary>
    public class FormSettings : Form
    {
        private readonly Form1 _mainForm;
        private List<LoadPortConfig> _configs;
        private readonly string _configPath;

        // 每行一组控件
        private readonly List<RowControls> _rows = new List<RowControls>();

        private class RowControls
        {
            public string Name;
            public RadioButton RbSerial;
            public RadioButton RbTcp;
            public Panel PanelSerial;
            public Panel PanelTcp;
            // 串口控件
            public ComboBox CbPort;
            public ComboBox CbBaud;
            public ComboBox CbData;
            public ComboBox CbParity;
            public ComboBox CbStop;
            // TCP 控件
            public TextBox TxtIp;
            public NumericUpDown NudPort;
            // Bypass
            public CheckBox ChkBypass;
        }

        public FormSettings(Form1 mainForm)
        {
            _mainForm = mainForm;
            _configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "loadport", "loadport_config.json");

            LoadConfig();
            BuildUI();
        }

        private void LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                _configs = JsonConvert.DeserializeObject<List<LoadPortConfig>>(json)
                           ?? new List<LoadPortConfig>();
            }
            else
            {
                _configs = new List<LoadPortConfig>();
            }
        }

        private void BuildUI()
        {
            this.Text = "通讯设置";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("微软雅黑", 9f);

            int formWidth = 700;
            int rowHeight = 200;
            int headerH = 50;
            int footerH = 50;
            int totalH = headerH + _configs.Count * rowHeight + footerH + 20;
            this.ClientSize = new Size(formWidth, Math.Max(totalH, 350));

            // ── 标题 ──────────────────────────────────────────────────
            var lblTitle = new Label
            {
                Text = "LoadPort 通讯参数设置",
                Font = new Font("微软雅黑", 12f, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            var lblNote = new Label
            {
                Text = "保存后自动重新连接设备",
                Font = new Font("微软雅黑", 8f),
                ForeColor = Color.Gray,
                Location = new Point(10, 32),
                AutoSize = true
            };
            this.Controls.Add(lblNote);

            // ── 每个 LoadPort 一个 GroupBox ────────────────────────────
            int y = headerH;
            foreach (var cfg in _configs)
            {
                var row = BuildRow(cfg, y);
                _rows.Add(row);
                y += rowHeight + 10;
            }

            // ── 底部按钮 ───────────────────────────────────────────────
            int btnY = y + 5;
            var btnSave = new Button
            {
                Text = "保存并重连",
                Location = new Point(formWidth - 230, btnY),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(formWidth - 120, btnY),
                Size = new Size(80, 32)
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            this.ClientSize = new Size(formWidth, btnY + 48);
        }

        private RowControls BuildRow(LoadPortConfig cfg, int y)
        {
            int left = 10;
            int w = this.ClientSize.Width - 20;

            var gb = new GroupBox
            {
                Text = cfg.Name,
                Location = new Point(left, y),
                Size = new Size(w, 190),
                Font = new Font("微软雅黑", 9f, FontStyle.Bold)
            };
            this.Controls.Add(gb);

            var row = new RowControls { Name = cfg.Name };

            // ── 模式选择 ─────────────────────────────────────────────
            row.RbSerial = new RadioButton { Text = "串口 (RS-232)", Location = new Point(10, 22), Width = 120, Font = new Font("微软雅黑", 9f) };
            row.RbTcp = new RadioButton { Text = "TCP/IP 以太网", Location = new Point(140, 22), Width = 130, Font = new Font("微软雅黑", 9f) };

            row.ChkBypass = new CheckBox { Text = "屏蔽（Bypass）", Location = new Point(290, 22), Width = 130, Font = new Font("微软雅黑", 9f), Checked = cfg.Bypass };

            gb.Controls.Add(row.RbSerial);
            gb.Controls.Add(row.RbTcp);
            gb.Controls.Add(row.ChkBypass);

            // ── 串口参数面板 ──────────────────────────────────────────
            row.PanelSerial = new Panel { Location = new Point(10, 48), Size = new Size(w - 25, 120) };

            void AddLabel(Panel p, string text, int lx, int ly) =>
                p.Controls.Add(new Label { Text = text, Location = new Point(lx, ly + 3), AutoSize = true, Font = new Font("微软雅黑", 8.5f) });

            // 串口号
            AddLabel(row.PanelSerial, "串口号", 0, 0);
            row.CbPort = new ComboBox { Location = new Point(55, 0), Width = 90, DropDownStyle = ComboBoxStyle.DropDown };
            row.CbPort.Items.AddRange(SerialPort.GetPortNames());
            SetSelected(row.CbPort, cfg.PortName);
            row.PanelSerial.Controls.Add(row.CbPort);

            // 波特率
            AddLabel(row.PanelSerial, "波特率", 160, 0);
            row.CbBaud = new ComboBox { Location = new Point(215, 0), Width = 85, DropDownStyle = ComboBoxStyle.DropDown };
            foreach (var b in new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 })
                row.CbBaud.Items.Add(b);
            SetSelected(row.CbBaud, cfg.BaudRate);
            row.PanelSerial.Controls.Add(row.CbBaud);

            // 数据位
            AddLabel(row.PanelSerial, "数据位", 315, 0);
            row.CbData = new ComboBox { Location = new Point(360, 0), Width = 55, DropDownStyle = ComboBoxStyle.DropDownList };
            row.CbData.Items.AddRange(new object[] { 5, 6, 7, 8 });
            SetSelected(row.CbData, cfg.DataBits);
            row.PanelSerial.Controls.Add(row.CbData);

            // 校验位
            AddLabel(row.PanelSerial, "校验", 0, 35);
            row.CbParity = new ComboBox { Location = new Point(40, 35), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            row.CbParity.Items.AddRange(Enum.GetNames(typeof(Parity)));
            row.CbParity.SelectedIndex = cfg.Parity < row.CbParity.Items.Count ? cfg.Parity : 0;
            row.PanelSerial.Controls.Add(row.CbParity);

            // 停止位
            AddLabel(row.PanelSerial, "停止位", 135, 35);
            row.CbStop = new ComboBox { Location = new Point(185, 35), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            row.CbStop.Items.AddRange(Enum.GetNames(typeof(StopBits)));
            row.CbStop.SelectedIndex = cfg.StopBits < row.CbStop.Items.Count ? cfg.StopBits : 1;
            row.PanelSerial.Controls.Add(row.CbStop);

            gb.Controls.Add(row.PanelSerial);

            // ── TCP 参数面板 ───────────────────────────────────────────
            row.PanelTcp = new Panel { Location = new Point(10, 48), Size = new Size(w - 25, 120), Visible = false };

            AddLabel(row.PanelTcp, "IP 地址", 0, 0);
            row.TxtIp = new TextBox { Location = new Point(60, 0), Width = 160, Text = cfg.TcpIp ?? "192.168.1.100" };
            row.PanelTcp.Controls.Add(row.TxtIp);

            AddLabel(row.PanelTcp, "端口", 240, 0);
            row.NudPort = new NumericUpDown { Location = new Point(270, 0), Width = 80, Minimum = 1, Maximum = 65535, Value = cfg.TcpPort > 0 ? cfg.TcpPort : 10001 };
            row.PanelTcp.Controls.Add(row.NudPort);

            var lblHint = new Label
            {
                Text = "提示：报文格式不变，以太网转串口服务器须将透传模式指向设备",
                ForeColor = Color.DimGray,
                Location = new Point(0, 35),
                Size = new Size(450, 18),
                Font = new Font("微软雅黑", 8f)
            };
            row.PanelTcp.Controls.Add(lblHint);

            gb.Controls.Add(row.PanelTcp);

            // ── RadioButton 切换面板显示 ─────────────────────────────
            row.RbSerial.CheckedChanged += (s, e) =>
            {
                row.PanelSerial.Visible = row.RbSerial.Checked;
                row.PanelTcp.Visible = !row.RbSerial.Checked;
            };

            // 设置初始状态
            if (cfg.CommMode == CommMode.Tcp)
            {
                row.RbTcp.Checked = true;
                row.PanelSerial.Visible = false;
                row.PanelTcp.Visible = true;
            }
            else
            {
                row.RbSerial.Checked = true;
            }

            return row;
        }

        private static void SetSelected(ComboBox cb, object value)
        {
            string s = value?.ToString() ?? "";
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i].ToString() == s)
                { cb.SelectedIndex = i; return; }
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 从控件回读配置
            foreach (var row in _rows)
            {
                var cfg = _configs.FirstOrDefault(c => c.Name == row.Name);
                if (cfg == null) continue;

                cfg.Bypass = row.ChkBypass.Checked;
                cfg.CommMode = row.RbTcp.Checked ? CommMode.Tcp : CommMode.Serial;

                if (cfg.CommMode == CommMode.Serial)
                {
                    cfg.PortName = row.CbPort.Text.Trim();
                    cfg.BaudRate = int.TryParse(row.CbBaud.Text, out int b) ? b : 9600;
                    cfg.DataBits = row.CbData.SelectedItem != null ? (int)row.CbData.SelectedItem : 8;
                    cfg.Parity = row.CbParity.SelectedIndex >= 0 ? row.CbParity.SelectedIndex : 0;
                    cfg.StopBits = row.CbStop.SelectedIndex >= 0 ? row.CbStop.SelectedIndex : 1;
                }
                else
                {
                    cfg.TcpIp = row.TxtIp.Text.Trim();
                    cfg.TcpPort = (int)row.NudPort.Value;
                }
            }

            // 保存 JSON
            try
            {
                string json = JsonConvert.SerializeObject(_configs, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                Logger.LogInfo("通讯配置已保存");
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存配置失败：" + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 通知主窗体重新连接
            _mainForm.InitLoadPorts();
            MessageBox.Show("设置已保存，设备重新连接完成。", "成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}
