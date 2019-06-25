using AmazonRank.Core;
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
    /// CBoxCountryCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class CBoxCountryCtrl : UserControl
    {
        public CBoxCountryCtrl()
        {
            InitializeComponent();

            var countryList = Utils.GetCountryComboBoxData();
            //if (countryList.Count <= 0)
            //{
            //    MessageBox.Show("请配置国家数据!");
            //    return;
            //}

            //proxyIpAddress = Utils.GetConfigValue("Proxy.IPAddress");

            this.CBox_Country.ItemsSource = countryList.ConvertAll(o => new
            {
                Key = o.CountryName
                ,
                Value = o
            });

            this.CBox_Country.SelectedIndex = 0;
        }

        /// <summary>
        /// 选中的值
        /// </summary>
        /// <returns></returns>
        public CountryModel SelectedValue
        {
            get
            {
                if (this.CBox_Country.SelectedIndex < 0)
                {
                    return null;
                }

                dynamic value = this.CBox_Country.SelectedValue;

                return value.Value as CountryModel;
            }
        }
    }
}
