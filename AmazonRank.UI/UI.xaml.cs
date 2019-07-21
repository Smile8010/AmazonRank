using AmazonRank.Core;
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
using System.Text.RegularExpressions;
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
        //string proxyIpAddress = string.Empty;


        public UI()
        {
            InitializeComponent();

            //List<string> countryList = getCountryKeys();
            //var countryList = Utils.GetCountryComboBoxData();
            //if (countryList.Count <= 0)
            //{
            //    MessageBox.Show("请配置国家数据!");
            //    return;
            //}

            //proxyIpAddress = Utils.GetConfigValue("Proxy.IPAddress");

            //this.CBox_Country.ItemsSource = countryList.ConvertAll(o => new
            //{
            //    Key = o.CountryName
            //    ,
            //    Value = new
            //    {
            //        o.Link,
            //        o.ZipCode,
            //        o.isProxy
            //    }
            //});

            //this.CBox_Country.SelectedIndex = 0;
        }



        private async void Btn_Query_Click(object sender, RoutedEventArgs e)
        {
            string asin = this.TBox_Asin.Text.Trim();
            if (IsNullOrEmpty(asin))
            {
                MessageBox.Show("输入ASIN!");
                return;
            }
            List<string> lines = Utils.GetLines(this.TBox_KeyWords.LineCount, i => this.TBox_KeyWords.GetLineText(i));
            int linesCount = lines.Count;
            if (linesCount <= 0)
            {
                MessageBox.Show("输入关键词!");
                return;
            }

            if (this.CBoxCountry.SelectedValue == null)
            {
                MessageBox.Show("选择站点!");
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
            var selectValue = this.CBoxCountry.SelectedValue;
            var clientResult = await Utils.GetInitCacheClientAsync(selectValue, OuputLine);
            if (clientResult.Success)
            {
                updateKwProcess(0, linesCount);
                string Link = selectValue.Link;
                List<SearchModel> queryResultList = new List<SearchModel>();
                // 启动线程处理队列
                await Task.Run(() =>
                 {
                     int current = 1;
                     Parallel.ForEach(lines, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, keyWord =>
                     {
                         SearchModel searchModel = seachKeyWordAsinRankAsync(clientResult.Data, new SearchModel
                         {
                             Asin = asin,
                             KeyWord = keyWord,
                             Link = Link
                         }).Result;
                         updateKwProcess(current++, linesCount);
                         string outputMsg = string.Empty;
                         if (!IsNullOrEmpty(searchModel.ErrorMsg))
                         {
                             outputMsg = $"关键词：【{searchModel.KeyWord}】,异常：{searchModel.ErrorMsg}";
                         }
                         else
                         {
                             if (searchModel.FindModels.Count <= 0)
                             {
                                 outputMsg = $"关键词：【{searchModel.KeyWord}】,没有找到对应的 Asin：【{searchModel.Asin}】";
                             }
                             else
                             {
                                 searchModel.FindModels.ForEach(l => outputMsg += $"搜索完成，关键词：【{l.KeyWord}】, Asin：【{searchModel.Asin}】,位置：【{l.Position}】，广告：【{(l.IsSponsored ? "是" : "否")}】");
                             }
                         }
                         queryResultList.Add(searchModel);
                         OuputLine(outputMsg);
                     });
                 });

                if (queryResultList.Count > 0)
                {
                    var dialogWin = new DialogWin();
                    dialogWin.Title = "搜索结果";
                    dialogWin.Width = 450;
                    dialogWin.Height = 200;
                    SResultUCtrl sRCtrl = new SResultUCtrl();
                    dialogWin.Container.Children.Add(sRCtrl);
                    sRCtrl.UpdateDataSource(queryResultList);
                    dialogWin.Show();
                }
            }

            setSearchStatus(true);


        }



        /// <summary>
        /// 获取搜索成功列表
        /// </summary>
        /// <param name="listTaskResult"></param>
        /// <param name="linesCount"></param>
        /// <returns></returns>
        private async Task<List<SearchModel>> getSearchModelListAsync(List<Task<Result<SearchModel>>> listTaskResult, int linesCount)
        {
            List<SearchModel> queryResultList = new List<SearchModel>();
            int current = 1;
            while (listTaskResult.Count > 0)
            {

                var searchResultList = await getResultSearchModelListAsync(listTaskResult);

                foreach (var searchResult in searchResultList)
                {
                    updateKwProcess(current++, linesCount);
                    if (!searchResult.Success)
                    {
                        OuputLine(searchResult.Msg);
                    }
                    else
                    {
                        string outputMsg = string.Empty;
                        var sModel = searchResult.Data;
                        if (sModel.FindModels.Count <= 0)
                        {
                            outputMsg = $"关键词：【{sModel.KeyWord}】,没有找到对应的 Asin：【{sModel.Asin}】";
                            //outputMsg = $"当前搜索完成，没有找到 关键词：【{sModel.KeyWord}】 对应的 Asin：【{sModel.Asin}】";
                        }
                        else
                        {
                            sModel.FindModels.ForEach(l => outputMsg += $"搜索完成，关键词：【{l.KeyWord}】, Asin：【{sModel.Asin}】,位置：【{l.Position}】，广告：【{(l.IsSponsored ? "是" : "否")}】");
                            //outputMsg = $"搜索完成，关键词：【{sModel.KeyWord}】, Asin：【{sModel.Asin}】,位置：【{sModel.Position}】，广告：【{(sModel.IsSponsored ? "是" : "否")}】,详情：【{sModel.DetailLink}】";
                        }
                        queryResultList.Add(sModel);
                        OuputLine(outputMsg);
                    }
                }
            }
            return queryResultList;
        }

        /// <summary>
        /// 获取搜索结果列表
        /// </summary>
        /// <param name="listTaskResult"></param>
        /// <returns></returns>
        private Task<List<Result<SearchModel>>> getResultSearchModelListAsync(List<Task<Result<SearchModel>>> listTaskResult)
        {
            return Task.Run(() =>
            {
                List<Result<SearchModel>> result = new List<Result<SearchModel>>();
                while (result.Count <= 0)
                {
                    var completedTaskList = listTaskResult.Where(o => o.IsCompleted).ToList();
                    foreach (var task in completedTaskList)
                    {
                        result.Add(task.Result);
                        listTaskResult.Remove(task);
                    }
                    System.Threading.Thread.Sleep(500);
                }
                return result;
            });
        }

        /// <summary>
        /// 设置搜索状态
        /// </summary>
        /// <param name="enabled"></param>
        private void setSearchStatus(bool enabled)
        {
            this.CBoxCountry.IsEnabled = enabled;
            this.TBox_Asin.IsEnabled = enabled;
            this.Btn_Query.IsEnabled = enabled;
            this.TBox_KeyWords.IsEnabled = enabled;
        }

        ///// <summary>
        ///// 文本输出
        ///// </summary>
        ///// <param name="msg"></param>
        //private void Ouput(string msg, bool isClear)
        //{
        //    if (isClear || this.TBox_Output.LineCount > 200)
        //    {
        //        this.TBox_Output.Clear();
        //        //this.TBox_Output.AppendText("清空缓存...\r\n");
        //    }
        //    this.TBox_Output.AppendText(msg);
        //    //自动滚动到底部
        //    this.TBox_Output.ScrollToEnd();
        //}

        /// <summary>
        /// 文本输出
        /// </summary>
        /// <param name="msg"></param>
        private void OuputLine(string msg, bool isClear = false)
        {
            this.TBoxOutput.Dispatcher.Invoke((Action)(() =>
            {
                this.TBoxOutput.OuputLine(msg, isClear);
            }));
        }

        /// <summary>
        /// 查询Asin关键词排名
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sModel"></param>
        /// <returns></returns>
        private async Task<SearchModel> seachKeyWordAsinRankAsync(HttpClient client, SearchModel sModel)
        {
            try
            {
                if (sModel.Page == 1)
                {
                    OuputLine($"关键词：【{sModel.KeyWord}】开始加载并检索第一页。");
                }
                else
                {
                    OuputLine($"关键词：【{sModel.KeyWord}】当前搜索页面：【{sModel.Page}/{sModel.TotalPage}】");
                }

                string requestURL = $"{sModel.Link}/s?k={sModel.KeyWord}";
                if (sModel.Page > 1)
                {
                    requestURL = $"{requestURL}&page={sModel.Page}";
                }
                var searchResponse = await client.GetAsync(new Uri(requestURL));

                searchResponse.EnsureSuccessStatusCode();

                string searchHtml = await searchResponse.Content.ReadAsStringAsync();

                Result<HtmlDocument> documentResult = await Utils.CheckIsAntiReptilePageAsync(searchHtml);
                //throw new Exception("故意异常");
                if (!documentResult.Success)
                {
                    sModel.ErrorMsg = "搜索失败：" + documentResult.Msg;
                    return sModel;
                    //return Result<SearchModel>.Error("搜索失败：" + documentResult.Msg);
                }

                string errorMsg = await getSearchFromDocumentAsync(documentResult.Data, sModel);

                if (!IsNullOrEmpty(errorMsg))
                {
                    sModel.ErrorMsg = errorMsg;
                    return sModel;
                    //return Result<SearchModel>.Error(errorMsg);
                }

                if (sModel.isFindedAsin || sModel.Page + 1 > sModel.TotalPage)
                {
                    return sModel;
                    //return Result<SearchModel>.OK("搜索完成", sModel);
                }

                sModel.Page++;
                return await seachKeyWordAsinRankAsync(client, sModel);

            }
            catch (Exception ex)
            {
                sModel.ErrorMsg = $"搜索异常：{ex.Message}";
                return sModel;
                //return Result<SearchModel>.Error($"搜索异常：{ex.Message}");
            }
        }

        ///// <summary>
        ///// 查询Asin关键词排名
        ///// </summary>
        ///// <param name="client"></param>
        ///// <param name="keyWord"></param>
        ///// <param name="Asin"></param>
        //private async Task<Result<SearchModel>> seachKeyWordAsinRankAsync(HttpClient client, SearchModel sModel)
        //{
        //    try
        //    {
        //        if (sModel.Page == 1)
        //        {
        //            OuputLine($"关键词：【{sModel.KeyWord}】开始加载并检索第一页。");
        //        }
        //        else
        //        {
        //            OuputLine($"关键词：【{sModel.KeyWord}】当前搜索页面：【{sModel.Page}/{sModel.TotalPage}】");
        //        }

        //        string requestURL = $"{sModel.Link}/s?k={sModel.KeyWord}";
        //        if (sModel.Page > 1)
        //        {
        //            requestURL = $"{requestURL}&page={sModel.Page}";
        //        }
        //        var searchResponse = await client.GetAsync(new Uri(requestURL));

        //        searchResponse.EnsureSuccessStatusCode();

        //        string searchHtml = await searchResponse.Content.ReadAsStringAsync();

        //        Result<HtmlDocument> documentResult = await Utils.CheckIsAntiReptilePageAsync(searchHtml);

        //        if (!documentResult.Success)
        //        {
        //            return Result<SearchModel>.Error("搜索失败：" + documentResult.Msg);
        //        }

        //        string errorMsg = await getSearchFromDocumentAsync(documentResult.Data, sModel);

        //        if (!IsNullOrEmpty(errorMsg))
        //        {
        //            return Result<SearchModel>.Error(errorMsg);
        //        }

        //        if (sModel.isFindedAsin || sModel.Page + 1 > sModel.TotalPage)
        //        {
        //            return Result<SearchModel>.OK("搜索完成", sModel);
        //        }

        //        sModel.Page++;
        //        return await seachKeyWordAsinRankAsync(client, sModel);

        //    }
        //    catch (Exception ex)
        //    {
        //        return Result<SearchModel>.Error($"搜索异常：{ex.Message}");
        //    }
        //}

        /// <summary>
        /// 从 document 中搜索数据
        /// </summary>
        /// <param name="document"></param>
        /// <param name="sModel"></param>
        /// <returns></returns>
        private Task<string> getSearchFromDocumentAsync(HtmlDocument document, SearchModel sModel)
        {
            return Task.Run(() =>
            {

                HtmlNode containNode = document.DocumentNode.SelectSingleNode("//*[@id='search']");

                if (containNode == null)
                {
                    return "找不到 id = search 的元素！";
                }

                if (sModel.Page == 1)
                {
                    // 赋值总页数
                    var pageLi = containNode.SelectNodes("./div[1]/div[2]/div/span[7]/div/div/div/ul/li");
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

                    // 获取搜索结果数
                    var resultNumNode = containNode.SelectSingleNode("./span/h1/div/div[1]/div/div/span[1]");
                    //1-48 of 588 results for
                    if (resultNumNode != null)
                    {
                        string pattern = @"of\s(?<num>[\w\W]+)\sresults";
                        var m = Regex.Match(resultNumNode.InnerText, pattern);

                        string numStr = m.Groups["num"].Value;
                        if (!IsNullOrEmpty(numStr))
                        {
                            sModel.ResultNumString = numStr;
                        }
                    }
                }

                var nodes = containNode.SelectNodes("//div[1]/div[2]/div/span[3]/div[1]/div");
                foreach (var node in nodes)
                {
                    sModel.Rank++;
                    string asin = node.Attributes["data-asin"].Value;
                    if (asin.Equals(sModel.Asin))
                    {
                        // a-row a-spacing-micro
                        // 查询是否广告
                        //var sponsoredNode = node.SelectSingleNode("./div/div/div/div/div/div[2]/div[2]/div/div[1]/div/span[1]");

                        var sponsoredNode = node.SelectSingleNode(".//div[@class='a-row a-spacing-micro']/span[1]");

                        // 查询详情地址
                        //var aNode = node.SelectSingleNode(".//a[@href]");

                        //sModel.PosIndex = pos;
                        //sModel.Position = $"Page:【{sModel.Page}】,Pos:【{pos}】,Rank:【{sModel.Rank}】";
                        //sModel.IsSponsored = sponsoredNode?.InnerText.Contains("Sponsored") ?? false;
                        //sModel.DetailLink = aNode?.Attributes["href"].Value ?? string.Empty;

                        //sModel.isFindedAsin = true;

                        int pos = Convert.ToInt32(node.Attributes["data-index"].Value) + 1;

                        sModel.FindModels.Add(new FindModel
                        {
                            IsSponsored = sponsoredNode?.InnerText.Contains("Sponsored") ?? false,
                            KeyWord = sModel.KeyWord,
                            Page = sModel.Page,
                            Pos = pos,
                            Rank = sModel.Rank,
                            ResultNumString = sModel.ResultNumString,
                            Position = $"Page:【{sModel.Page}】,Pos:【{pos}】,Rank:【{sModel.Rank}】"
                        });

                        if (sModel.isFindedAsin)
                        {
                            break;
                        }
                    }
                }

                return string.Empty;
            });

        }

        /// <summary>
        /// 更新关键词搜索进度
        /// </summary>
        /// <param name="current"></param>
        /// <param name="total"></param>
        private void updateKwProcess(int current, int total)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.Label_KwProcess.Content = $"{current}/{total}";
                this.ProgressBar_KwProcess.Maximum = total;
                this.ProgressBar_KwProcess.Value = current;
            }));

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // 销毁 HttpClient
            Utils.ClearClientCache();
        }
    }

}
