using AmazonRank.Core;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AmazonRank.UI.UserWins
{
    /// <summary>
    /// AsinCategory.xaml 的交互逻辑
    /// </summary>
    public partial class AsinCategory : Window
    {
        string proxyIpAddress = string.Empty;

        public AsinCategory()
        {
            InitializeComponent();

        }

        private async void Btn_Query_Click(object sender, RoutedEventArgs e)
        {
            var countrySelectedValue = this.CBoxCountry.SelectedValue;
            if (countrySelectedValue == null)
            {
                MessageBox.Show("请选择站点");
                return;
            }

            List<string> lines = Utils.GetLines(this.TBox_Asin.LineCount, i => this.TBox_Asin.GetLineText(i));
            int lineCount = lines.Count;
            if (lineCount <= 0)
            {
                MessageBox.Show("请输入要查询的Asin");
                return;
            }

            using (HttpClient client = Utils.GetDefaultHttpClient(countrySelectedValue.Value.isProxy))
            {
                OuputLine($"初始化加载器...", true);
                Result<object> initResult = await Utils.InitQueryAsync(client, countrySelectedValue.Value.Link, countrySelectedValue.Value.ZipCode);
                if (!initResult.Success)
                {
                    OuputLine(initResult.Msg);
                }
                else
                {
                    //https://www.amazon.com/gp/product/B07KY56KJ2?psc=1
                }
            }
        }

        /// <summary>
        /// 文本输出
        /// </summary>
        /// <param name="msg"></param>
        private void OuputLine(string msg, bool isClear = false)
        {
            this.TBoxOutput.OuputLine(msg, isClear);
        }

        /// <summary>
        /// 搜索信息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="link"></param>
        /// <param name="Asin"></param>
        /// <returns></returns>
        //private async Task<Result<AsinCategoryTreeModel>> searchInfoAsync(HttpClient client, string link, string asin)
        //{
        //    try
        //    {
        //        OuputLine($"开始搜索Asin;【{asin}】");

        //        string requestURL = $"{link}/gp/product/{asin}?psc=1";

        //        var searchResponse = await client.GetAsync(new Uri(requestURL));

        //        searchResponse.EnsureSuccessStatusCode();

        //        string searchHtml = await searchResponse.Content.ReadAsStringAsync();

        //        Result<HtmlDocument> documentResult = await Utils.CheckIsAntiReptilePageAsync(searchHtml);

        //        if (!documentResult.Success)
        //        {
        //            return Result<AsinCategoryTreeModel>.Error("搜索失败：" + documentResult.Msg);
        //        }

        //        //HtmlDocument document = documentResult.Data;



        //    }
        //    catch (Exception ex)
        //    {
        //        return Result<AsinCategoryTreeModel>.Error($"搜索异常：{ex.Message}");
        //    }

        //}

        /// <summary>
        /// 从 document 中获取信息
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private Task<Result<AsinCategoryTreeModel>> getInfoFromDocumentAsync(HtmlDocument document, string asin)
        {
            return Task.Run(() =>
            {
                AsinCategoryTreeModel model = new AsinCategoryTreeModel() { DisplayName = asin, Level = -1 };
                HtmlNode salesRankNode = document.DocumentNode.SelectSingleNode("//*[@id='SalesRank']");
                if (salesRankNode != null)
                {
                    if (salesRankNode.OriginalName.ToUpper().Equals("LI"))
                    {
                        var nodes = salesRankNode.ChildNodes;

                    }
                }

                return Result<AsinCategoryTreeModel>.OK(model);
            });
        }
    }
}
