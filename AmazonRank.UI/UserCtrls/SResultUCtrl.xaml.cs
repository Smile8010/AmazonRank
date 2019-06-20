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
    /// SResultUCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class SResultUCtrl : UserControl
    {
        public SResultUCtrl()
        {
            InitializeComponent();
        }

        public void UpdateDataSource(List<SearchModel> sModelList)
        {
            if (sModelList != null && sModelList.Count > 0)
            {
                this.Lb_Title.Content =$"Asin：{sModelList[0].Asin}";
                var source = new List<dynamic>();
                sModelList.ForEach(sModel =>
                {
                    source.Add(new
                    {
                        sModel.KeyWord,
                        sModel.Page,
                        sModel.Rank,
                        IsSponsoredText=sModel.SResult.IsSponsored?"是":"否",
                        Pos = sModel.SResult.PosIndex
                    });
                });
                this.DGrid_SResult.ItemsSource = source;
            }
        }
    }
}
