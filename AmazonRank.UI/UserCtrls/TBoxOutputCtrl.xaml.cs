using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AmazonRank.UI.UserCtrls
{
    /// <summary>
    /// TBoxOutputCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class TBoxOutputCtrl : UserControl
    {
        int MaxLineCount { get; set; } = 200;

        public TBoxOutputCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 文本输出
        /// </summary>
        /// <param name="msg"></param>
        public void Ouput(string msg, bool isClear)
        {
            if (isClear || this.TBox_Output.LineCount > MaxLineCount)
            {
                this.TBox_Output.Clear();
                //this.TBox_Output.AppendText("清空缓存...\r\n");
            }
            this.TBox_Output.AppendText(msg);
            //自动滚动到底部
            this.TBox_Output.ScrollToEnd();
        }

        /// <summary>
        /// 文本输出
        /// </summary>
        /// <param name="msg"></param>
        public void OuputLine(string msg, bool isClear = false)
        {
            Ouput($"{DateTime.Now.ToString("MMdd-HH:mm:ss")}：{msg}\r\n", isClear);
        }
    }
}
