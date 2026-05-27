namespace LoadPort
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.lst_Log = new System.Windows.Forms.ListView();
            this.infoName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.info = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.flowLayoutPanelMain = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlccon = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Alarm.png");
            this.imageList1.Images.SetKeyName(1, "FA_OnlineRemote.png");
            this.imageList1.Images.SetKeyName(2, "Home.ico");
            // 
            // lst_Log
            // 
            this.lst_Log.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.infoName,
            this.info});
            this.lst_Log.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lst_Log.Font = new System.Drawing.Font("黑体", 10.5F);
            this.lst_Log.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lst_Log.HideSelection = false;
            this.lst_Log.Location = new System.Drawing.Point(0, 627);
            this.lst_Log.Name = "lst_Log";
            this.lst_Log.Size = new System.Drawing.Size(937, 241);
            this.lst_Log.SmallImageList = this.imageList1;
            this.lst_Log.TabIndex = 10;
            this.lst_Log.UseCompatibleStateImageBehavior = false;
            this.lst_Log.View = System.Windows.Forms.View.Details;
            // 
            // infoName
            // 
            this.infoName.Text = "日志名称";
            this.infoName.Width = 28;
            // 
            // info
            // 
            this.info.Text = "日志信息";
            this.info.Width = 900;
            // 
            // flowLayoutPanelMain
            // 
            this.flowLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Left;
            this.flowLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelMain.Name = "flowLayoutPanelMain";
            this.flowLayoutPanelMain.Size = new System.Drawing.Size(862, 627);
            this.flowLayoutPanelMain.TabIndex = 11;
            // 
            // lblPlccon
            // 
            this.lblPlccon.BackColor = System.Drawing.Color.OrangeRed;
            this.lblPlccon.Font = new System.Drawing.Font("黑体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPlccon.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.lblPlccon.Location = new System.Drawing.Point(868, 7);
            this.lblPlccon.Margin = new System.Windows.Forms.Padding(3);
            this.lblPlccon.Name = "lblPlccon";
            this.lblPlccon.Size = new System.Drawing.Size(69, 25);
            this.lblPlccon.TabIndex = 12;
            this.lblPlccon.Text = "PLC未连接";
            this.lblPlccon.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(869, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 12);
            this.label1.TabIndex = 13;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(937, 868);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblPlccon);
            this.Controls.Add(this.flowLayoutPanelMain);
            this.Controls.Add(this.lst_Log);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ListView lst_Log;
        private System.Windows.Forms.ColumnHeader infoName;
        private System.Windows.Forms.ColumnHeader info;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelMain;
        private System.Windows.Forms.Label lblPlccon;
        private System.Windows.Forms.Label label1;
    }
}

