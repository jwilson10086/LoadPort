using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LoadPort
{
    // 1. 自定义窗体类的代码
    public partial class CustomMessageBox : Form
    {
        // 构造函数，接收要显示的消息
        public CustomMessageBox(string message)
        {
            InitializeComponent();
            
            lblMessage.Left = (this.ClientSize.Width - lblMessage.Width) / 2;

            lblMessage.Text = message; // 将消息文本赋给Label
        }

        // "确定"按钮的点击事件
        private void btnOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK; // 设置对话框结果为OK
            this.Close(); // 关闭窗体
        }
        // "取消"按钮的点击事件
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; 
            this.Close(); // 关闭窗体
        }

        // 静态方法，用于方便地显示弹窗
        public static DialogResult Show(string message)
        {
            using (var form = new CustomMessageBox(message))
            {
                return form.ShowDialog(); // 以对话框模式显示窗体
            }
        }
    }
}
