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

            using (HttpClient client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip }))
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
                dynamic selectValue = this.CBox_Country.SelectedValue;
                Result<object> initResult = await initQuery(client, selectValue.Link, selectValue.ZipCode);
                if (!initResult.Success)
                {
                    Ouput(initResult.Msg);
                    return;
                }


            }
        }

        /// <summary>
        /// 文本输出
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="isClear"></param>
        private void Ouput(string msg, bool isClear = false)
        {
            if (isClear)
            {
                this.TBox_Output.Clear();
            }
            this.TBox_Output.AppendText(msg + Environment.NewLine);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="link"></param>
        /// <param name="zipCode"></param>
        private async Task<Result<object>> initQuery(HttpClient client, string link, string zipCode)
        {
            try
            {
                var indexHtmlResponse = await client.GetAsync(link);
                indexHtmlResponse.EnsureSuccessStatusCode();

                string indexHtml = await indexHtmlResponse.Content.ReadAsStringAsync();
                HtmlDocument document = new HtmlDocument();

                await Task.Run(() => document.LoadHtml(indexHtml));

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
        //private async Task<Result<object>> seachKeyWordAsinRank(HttpClient client,string link, string keyWord, string Asin,int page = 1)
        //{
        //    try
        //    {
        //        string requestURL = $"{link}/s?k={keyWord}";
        //        var searchResponse = await client.GetAsync(requestURL);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

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
    }
}
