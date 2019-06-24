using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AmazonRank.Core
{
    public class Utils
    {
        /// <summary>
        /// 初始化查询（请求页面，设置当前地址）
        /// </summary>
        /// <param name="client"></param>
        /// <param name="link"></param>
        /// <param name="zipCode"></param>
        /// <returns></returns>
        public static async Task<Result<object>> InitQueryAsync(HttpClient client, string link, string zipCode)
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
        /// 检查是否反爬虫界面
        /// </summary>
        /// <param name="html">html内容</param>
        /// <returns></returns>
        public static Task<Result<HtmlDocument>> CheckIsAntiReptilePageAsync(string html)
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
    }
}
