using AmazonRank.UI.UserCtrls;
using AmazonRank.UI.UserWins;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
using static System.String;

namespace AmazonRank.UI
{

    /// <summary>
    /// UI.xaml 的交互逻辑
    /// </summary>
    public partial class UI : Window
    {

        public UI()
        {
            InitializeComponent();
            this.CBox_Country.ItemsSource = new List<dynamic>
            {
               new { Key="美",Value = new { Link="https://www.amazon.com",ZipCode="10041" }  }
            };

            this.CBox_Country.SelectedIndex = 0;
        }

        private async void Btn_Query_Click(object sender, RoutedEventArgs e)
        {
            string asin = this.TBox_Asin.Text.Trim();
            if (IsNullOrEmpty(asin))
            {
                MessageBox.Show("输入ASIN!");
                return;
            }
            List<string> lines = getLinesText(this.TBox_KeyWords);
            if (lines.Count <= 0)
            {
                MessageBox.Show("输入关键字!");
                return;
            }

            if (this.CBox_Country.SelectedIndex < 0)
            {
                MessageBox.Show("选择国家!");
                return;
            }

            //var dialogWin1 = new DialogWin();
            //dialogWin1.Title = "搜索结果";
            //SResultUCtrl srUCtrl1 = new SResultUCtrl();
            //srUCtrl1.UpdateDataSource(new List<SearchModel> {
            //    new SearchModel
            //    {
            //     Asin=asin,
            //     KeyWord="ddd",
            //     SResult = new SearchResult()
            //    }
            //});
            //dialogWin1.Container.Children.Add(srUCtrl1);
            //dialogWin1.ShowDialog();

            //return;

            setSearchStatus(false);

            using (HttpClient client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip }))
            {
                client.Timeout = new TimeSpan(0, 0, 1, 0);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
                dynamic selectValue = this.CBox_Country.SelectedValue;
                OuputLine($"初始化加载器...");
                Result<object> initResult = await initQueryAsyn(client, selectValue.Value.Link, selectValue.Value.ZipCode);
                if (!initResult.Success)
                {
                    OuputLine(initResult.Msg);
                    return;
                }

                string Link = selectValue.Value.Link;

                //Parallel.ForEach(lines,new ParallelOptions { MaxDegreeOfParallelism=5 },())
                List<SearchModel> queryResultList = new List<SearchModel>();
                foreach (var kewWords in lines)
                {
                    OuputLine($"开始搜索关键字：【{kewWords}】");
                    var searchResult = await seachKeyWordAsinRankAsync(client, new SearchModel
                    {
                        Asin = asin,
                        KeyWord = kewWords,
                        Link = Link
                    });

                    if (!searchResult.Success)
                    {
                        OuputLine(searchResult.Msg);
                    }
                    else
                    {
                        string outputMsg = string.Empty;
                        var sModel = searchResult.Data;
                        if (sModel.SResult == null)
                        {
                            outputMsg = $"当前搜索完成，没有找到 关键字：【{kewWords}】 对应的 Asin：【{asin}】";
                        }
                        else
                        {
                            outputMsg = $"搜索完成，关键字：【{kewWords}】, Asin：【{asin}】,位置：【{sModel.SResult.Position}】，广告：【{(sModel.SResult.IsSponsored ? "是" : "否")}】,详情：【{sModel.SResult.DetailLink}】";
                            queryResultList.Add(sModel);
                        }

                        OuputLine(outputMsg);
                    }
                }

                setSearchStatus(true);

                if (queryResultList.Count > 0)
                {
                    var dialogWin = new DialogWin();
                    dialogWin.Title = "搜索结果";
                    dialogWin.Width = 450;
                    dialogWin.Height = 200;
                    SResultUCtrl srUCtrl = new SResultUCtrl();
                    srUCtrl.UpdateDataSource(queryResultList);
                    dialogWin.Container.Children.Add(srUCtrl);
                    dialogWin.Show();
                }
            }
        }

        /// <summary>
        /// 设置搜索状态
        /// </summary>
        /// <param name="enabled"></param>
        private void setSearchStatus(bool enabled)
        {
            this.CBox_Country.IsEnabled = enabled;
            this.TBox_Asin.IsEnabled = enabled;
            this.Btn_Query.IsEnabled = enabled;
            this.TBox_KeyWords.IsEnabled = enabled;
        }

        /// <summary>
        /// 文本输出
        /// </summary>
        /// <param name="msg"></param>
        private void Ouput(string msg)
        {
            if (this.TBox_Output.LineCount > 200)
            {
                this.TBox_Output.Clear();
                this.TBox_Output.AppendText("清空缓存...\r\n");
            }
            this.TBox_Output.AppendText(msg);
        }

        /// <summary>
        /// 文本输出
        /// </summary>
        /// <param name="msg"></param>
        private void OuputLine(string msg)
        {
            Ouput(msg + "\r\n");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="link"></param>
        /// <param name="zipCode"></param>
        private async Task<Result<object>> initQueryAsyn(HttpClient client, string link, string zipCode)
        {
            try
            {
                var indexHtmlResponse = await client.GetAsync(link);
                indexHtmlResponse.EnsureSuccessStatusCode();

                string indexHtml = await indexHtmlResponse.Content.ReadAsStringAsync();

                HtmlDocument document = await CheckIsAntiReptilePageAsync(indexHtml);

                //HtmlDocument document = new HtmlDocument();

                //await Task.Run(() => document.LoadHtml(indexHtml));

                //设置地区
                List<KeyValuePair<string, string>> paramsList = new List<KeyValuePair<string, string>>() {
                    new KeyValuePair<string, string>("locationType","LOCATION_INPUT"),
                    new KeyValuePair<string, string>("zipCode",zipCode),
                    new KeyValuePair<string, string>("storeContext","generic"),
                    new KeyValuePair<string, string>("deviceType","web"),
                    new KeyValuePair<string, string>("pageType","Gateway"),
                    new KeyValuePair<string, string>("actionSource","glow")
                };

                var setResponse = await client.PostAsync($"{link}/gp/delivery/ajax/address-change.html", new FormUrlEncodedContent(paramsList));
                setResponse.EnsureSuccessStatusCode();

                var responseText = await setResponse.Content.ReadAsStringAsync();

                var data = new { isValidAddress = 0 };

                data = JsonConvert.DeserializeAnonymousType(responseText, data);
                if (data == null || data.isValidAddress != 1)
                {
                    return Result<object>.Error("无法设置收货目的地！");
                }

                return Result<object>.OK();
            }
            catch (Exception ex)
            {
                return Result<object>.Error($"初始化查询异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 查询Asin关键字排名
        /// </summary>
        /// <param name="client"></param>
        /// <param name="keyWord"></param>
        /// <param name="Asin"></param>
        private async Task<Result<SearchModel>> seachKeyWordAsinRankAsync(HttpClient client, SearchModel sModel)
        {
            try
            {
                string requestURL = $"{sModel.Link}/s?k={sModel.KeyWord}";
                if (sModel.Page > 1)
                {
                    requestURL = $"{requestURL}&page={sModel.Page}";
                }
                var searchResponse = await client.GetAsync(new Uri(requestURL));

                searchResponse.EnsureSuccessStatusCode();

                string searchHtml = await searchResponse.Content.ReadAsStringAsync();

                HtmlDocument document = await CheckIsAntiReptilePageAsync(searchHtml);


                HtmlNode containNode = document.DocumentNode.SelectSingleNode("//*[@id='search']");

                if (containNode == null)
                {
                    return Result<SearchModel>.Error("找不到 id = search 的元素！");
                }

                if (sModel.Page == 1)
                {
                    // 赋值总页数
                    var pageLi = containNode.SelectNodes("//div[1]/div[2]/div/span[7]/div/div/div/ul/li");
                    if (pageLi != null)
                    {
                        if (pageLi.Count > 2)
                        {
                            var totalNode = pageLi[pageLi.Count - 2];
                            int totalPage = 0;
                            if (int.TryParse(totalNode.InnerText, out totalPage))
                            {
                                sModel.TotalPage = totalPage;
                            }
                        }

                    }
                }

                var nodes = containNode.SelectNodes("//div[1]/div[2]/div/span[3]/div[1]/div");
                int pos = 0;
                foreach (var node in nodes)
                {
                    sModel.Rank++;
                    pos++;
                    string asin = node.Attributes["data-asin"].Value;
                    if (asin.Equals(sModel.Asin))
                    {
                        // 查询是否广告
                        var sponsoredNode = node.SelectSingleNode("//div/div/div/div/div/div[2]/div[2]/div/div[1]/div/span[1]");

                        // 查询详情地址
                        var aNode = node.SelectSingleNode("//div/div/div/div/div/div[2]/div[1]/div/div/span/a");

                        sModel.SResult = new SearchResult
                        {
                            PosIndex = pos,
                            Position = $"Page:【{sModel.Page}】,Pos:【{pos}】,Rank:【{sModel.Rank}】",
                            IsSponsored = sponsoredNode?.InnerText.Contains("Sponsored") ?? false,
                            DetailLink = aNode?.Attributes["href"].Value ?? string.Empty
                        };
                    }
                }
                sModel.Page++;

                if (sModel.SResult != null || sModel.Page > sModel.TotalPage)
                {
                    return Result<SearchModel>.OK("搜索完成", sModel);
                }


                return await seachKeyWordAsinRankAsync(client, sModel);

            }
            catch (Exception ex)
            {
                return Result<SearchModel>.Error($"搜索异常：{ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有行文本
        /// </summary>
        /// <param name="tBox"></param>
        /// <returns></returns>
        private List<string> getLinesText(TextBox tBox)
        {
            List<string> lines = new List<string>();
            int lineCount = tBox.LineCount;
            if (lineCount > 0)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    string lineText = tBox.GetLineText(i).Trim();
                    if (!IsNullOrEmpty(lineText))
                    {
                        lines.Add(lineText);
                    }
                }
            }
            return lines;
        }

        /// <summary>
        /// 检查是否反爬虫界面
        /// </summary>
        private async Task<HtmlDocument> CheckIsAntiReptilePageAsync(string html)
        {
            HtmlDocument document = new HtmlDocument();
            await Task.Run(() => document.LoadHtml(html));
            var inputNodes = document.DocumentNode.SelectSingleNode("//input[@id='captchacharacters']");
            if (inputNodes != null && (inputNodes.Attributes["name"]?.Value ?? "").Equals("field-keywords")) {
                throw new Exception("被Amazon反爬虫拦截了，获取失败，请等一段时间后再重试！");
            }
            return document;
        }
    }
}
