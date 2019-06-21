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
        string proxyIpAddress = string.Empty;
        public UI()
        {
            InitializeComponent();

            //List<string> countryList = getCountryKeys();
            var countryList = getCountryComboBoxData();
            if (countryList.Count <= 0)
            {
                MessageBox.Show("请配置国家数据!");
                return;
            }

            proxyIpAddress = getConfigValue("Proxy.IPAddress");

            this.CBox_Country.ItemsSource = countryList.ConvertAll(o => new
            {
                Key = o.CountryName
                ,
                Value = new
                {
                    o.Link,
                    o.ZipCode,
                    o.isProxy
                }
            });

            this.CBox_Country.SelectedIndex = 0;
        }

        /// <summary>
        /// 获取国家下拉框数据
        /// </summary>
        /// <returns></returns>
        private List<CountryModel> getCountryComboBoxData()
        {
            // 获取下拉框配置
            string countryKeyStr = getConfigValue("Country.CfgKeys");
            var countryList = countryKeyStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<CountryModel> list = new List<CountryModel>();

            countryList.ForEach(key =>
            {
                string countryDataStr = getConfigValue($"Country.{key}");
                if (!IsNullOrEmpty(countryDataStr))
                {
                    CountryModel country = DeserializeObject<CountryModel>(countryDataStr);
                    if (country != null)
                    {
                        list.Add(country);
                    }
                }
            });

            return list;
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
            int linesCount = lines.Count;
            if (linesCount <= 0)
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

            dynamic selectValue = this.CBox_Country.SelectedValue;
            HttpClientHandler handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
            if (selectValue.Value.isProxy && !IsNullOrEmpty(proxyIpAddress))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyIpAddress);
            };
            using (HttpClient client = new HttpClient(handler))
            {
                client.Timeout = new TimeSpan(0, 0, 1, 0);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");

                OuputLine($"初始化加载器...", true);
                updateKwProcess(0, linesCount);
                Result<object> initResult = await initQueryAsyn(client, selectValue.Value.Link, selectValue.Value.ZipCode);
                if (!initResult.Success)
                {
                    OuputLine(initResult.Msg);
                    return;
                }

                string Link = selectValue.Value.Link;

                //Parallel.ForEach(lines,new ParallelOptions { MaxDegreeOfParallelism=5 },())
                //List<SearchModel> queryResultList = new List<SearchModel>();
                //int current = 1;
                List<Task<Result<SearchModel>>> listTaskResult = new List<Task<Result<SearchModel>>>();
                foreach (var kewWords in lines)
                {

                    OuputLine($"开始搜索关键字：【{kewWords}】");
                    listTaskResult.Add(seachKeyWordAsinRankAsync(client, new SearchModel
                    {
                        Asin = asin,
                        KeyWord = kewWords,
                        Link = Link
                    }));
                }

                var queryResultList = await getSearchModelListAsync(listTaskResult, linesCount);

                //while (listTaskResult.Count > 0)
                //{
                //    foreach (var task in listTaskResult.Where(o => o.IsCompleted))
                //    {
                //        var searchResult = await task;
                //        updateKwProcess(current++, linesCount);
                //        if (!searchResult.Success)
                //        {
                //            OuputLine(searchResult.Msg);
                //        }
                //        else
                //        {
                //            string outputMsg = string.Empty;
                //            var sModel = searchResult.Data;
                //            if (sModel.SResult == null)
                //            {
                //                outputMsg = $"当前搜索完成，没有找到 关键字：【{sModel.KeyWord}】 对应的 Asin：【{asin}】";
                //            }
                //            else
                //            {
                //                outputMsg = $"搜索完成，关键字：【{sModel.KeyWord}】, Asin：【{asin}】,位置：【{sModel.SResult.Position}】，广告：【{(sModel.SResult.IsSponsored ? "是" : "否")}】,详情：【{sModel.SResult.DetailLink}】";
                //                queryResultList.Add(sModel);
                //            }

                //            OuputLine(outputMsg);
                //        }
                //        listTaskResult.Remove(task);
                //    }
                //}

                //foreach (var task in listTaskResult)
                //{
                //    var searchResult = await task;
                //    updateKwProcess(current++, linesCount);
                //    if (!searchResult.Success)
                //    {
                //        OuputLine(searchResult.Msg);
                //    }
                //    else
                //    {
                //        string outputMsg = string.Empty;
                //        var sModel = searchResult.Data;
                //        if (sModel.SResult == null)
                //        {
                //            outputMsg = $"当前搜索完成，没有找到 关键字：【{sModel.KeyWord}】 对应的 Asin：【{asin}】";
                //        }
                //        else
                //        {
                //            outputMsg = $"搜索完成，关键字：【{sModel.KeyWord}】, Asin：【{asin}】,位置：【{sModel.SResult.Position}】，广告：【{(sModel.SResult.IsSponsored ? "是" : "否")}】,详情：【{sModel.SResult.DetailLink}】";
                //            queryResultList.Add(sModel);
                //        }

                //        OuputLine(outputMsg);
                //    }
                //}

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
                        if (!sModel.isFindedAsin)
                        {
                            outputMsg = $"当前搜索完成，没有找到 关键字：【{sModel.KeyWord}】 对应的 Asin：【{sModel.Asin}】";
                        }
                        else
                        {
                            outputMsg = $"搜索完成，关键字：【{sModel.KeyWord}】, Asin：【{sModel.Asin}】,位置：【{sModel.Position}】，广告：【{(sModel.IsSponsored ? "是" : "否")}】,详情：【{sModel.DetailLink}】";
                            queryResultList.Add(sModel);
                        }

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
            this.CBox_Country.IsEnabled = enabled;
            this.TBox_Asin.IsEnabled = enabled;
            this.Btn_Query.IsEnabled = enabled;
            this.TBox_KeyWords.IsEnabled = enabled;
        }

        /// <summary>
        /// 文本输出
        /// </summary>
        /// <param name="msg"></param>
        private void Ouput(string msg, bool isClear)
        {
            if (isClear || this.TBox_Output.LineCount > 200)
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
        private void OuputLine(string msg, bool isClear = false)
        {
            Ouput($"{DateTime.Now.ToString("MMdd-HH:mm:ss")}：{msg}\r\n", isClear);
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

                Result<HtmlDocument> documentResult = await CheckIsAntiReptilePageAsync(indexHtml);

                if (!documentResult.Success)
                {
                    return Result<object>.Error("初始化失败：" + documentResult.Msg);
                }

                //HtmlDocument document = documentResult.Data;

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

                Result<HtmlDocument> documentResult = await CheckIsAntiReptilePageAsync(searchHtml);

                if (!documentResult.Success)
                {

                    return Result<SearchModel>.Error("搜索失败：" + documentResult.Msg);
                }

                HtmlDocument document = documentResult.Data;

                string errorMsg = await getSearchFromDocumentAsync(document, sModel);

                if (!IsNullOrEmpty(errorMsg))
                {
                    return Result<SearchModel>.Error(errorMsg);
                }

                if (sModel.isFindedAsin || sModel.Page + 1 > sModel.TotalPage)
                {
                    return Result<SearchModel>.OK("搜索完成", sModel);
                }

                sModel.Page++;
                return await seachKeyWordAsinRankAsync(client, sModel);

            }
            catch (Exception ex)
            {
                return Result<SearchModel>.Error($"搜索异常：{ex.Message}");
            }
        }

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
                int pos = 0;
                foreach (var node in nodes)
                {
                    sModel.Rank++;
                    pos++;
                    string asin = node.Attributes["data-asin"].Value;
                    if (asin.Equals(sModel.Asin))
                    {
                        // a-row a-spacing-micro
                        // 查询是否广告
                        //var sponsoredNode = node.SelectSingleNode("./div/div/div/div/div/div[2]/div[2]/div/div[1]/div/span[1]");

                        var sponsoredNode = node.SelectSingleNode(".//div[@class='a-row a-spacing-micro']/span[1]");

                        // 查询详情地址
                        var aNode = node.SelectSingleNode(".//a[@href]");

                        sModel.PosIndex = pos;
                        sModel.Position = $"Page:【{sModel.Page}】,Pos:【{pos}】,Rank:【{sModel.Rank}】";
                        sModel.IsSponsored = sponsoredNode?.InnerText.Contains("Sponsored") ?? false;
                        sModel.DetailLink = aNode?.Attributes["href"].Value ?? string.Empty;

                        sModel.isFindedAsin = true;

                        //sModel.SResult = new SearchResult
                        //{
                        //    PosIndex = pos,
                        //    Position = $"Page:【{sModel.Page}】,Pos:【{pos}】,Rank:【{sModel.Rank}】",
                        //    IsSponsored = sponsoredNode?.InnerText.Contains("Sponsored") ?? false,
                        //    DetailLink = aNode?.Attributes["href"].Value ?? string.Empty
                        //};
                        break;
                    }
                }

                return string.Empty;
            });

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
        private Task<Result<HtmlDocument>> CheckIsAntiReptilePageAsync(string html)
        {
            return Task.Run(() =>
            {
                try
                {
                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(html);
                    var inputNodes = document.DocumentNode.SelectSingleNode("//input[@id='captchacharacters']");
                    if (inputNodes != null && (inputNodes.Attributes["name"]?.Value ?? "").Equals("field-keywords"))
                    {
                        return Result<HtmlDocument>.Error("被Amazon反爬虫拦截了，获取失败，请等一段时间后再重试！");
                        //throw new Exception("被Amazon反爬虫拦截了，获取失败，请等一段时间后再重试！");
                    }
                    return Result<HtmlDocument>.OK(document);
                }
                catch (Exception ex)
                {
                    return Result<HtmlDocument>.Error(ex.Message);
                }
            });
            //var document = await Task.Run(() =>
            //{
            //    HtmlDocument document1 = new HtmlDocument();
            //    document1.LoadHtml(html);
            //    return document1;
            //});
            //var inputNodes = document.DocumentNode.SelectSingleNode("//input[@id='captchacharacters']");
            //if (inputNodes != null && (inputNodes.Attributes["name"]?.Value ?? "").Equals("field-keywords"))
            //{
            //    throw new Exception("被Amazon反爬虫拦截了，获取失败，请等一段时间后再重试！");
            //}
            //return document;
        }

        /// <summary>
        /// 更新关键词搜索进度
        /// </summary>
        /// <param name="current"></param>
        /// <param name="total"></param>
        private void updateKwProcess(int current, int total)
        {
            this.Label_KwProcess.Content = $"{current}/{total}";
            this.ProgressBar_KwProcess.Maximum = total;
            this.ProgressBar_KwProcess.Value = current;
        }


        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <param name="configKey"></param>
        /// <returns></returns>
        private string getConfigValue(string configKey, string defaultValue = "")
        {
            if (IsNullOrEmpty(configKey)) { return defaultValue; }
            return System.Configuration.ConfigurationManager.AppSettings[configKey] ?? defaultValue;
        }

        /// <summary>
        /// 字符串转实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        private T DeserializeObject<T>(string jsonStr) where T : class
        {
            try
            {
                T entity = JsonConvert.DeserializeObject<T>(jsonStr);
                return entity;
            }
            catch
            {
                return null;
            }
        }
    }

    //public class ProcessLabel
    //{
    //    public ProcessLabel(Label label, int total) : this(label, 1, total)
    //    {
    //    }

    //    public ProcessLabel(Label label, int current, int total)
    //    {
    //        this.label = label;
    //        this.current = current;
    //        this.total = total;
    //    }

    //    private Label label;

    //    private int current;

    //    private int total;

    //    /// <summary>
    //    /// 进行中
    //    /// </summary>
    //    public void UpdateProcessText()
    //    {
    //        label.Content = $"{this.current++}/{this.total}";
    //    }
    //}
}
