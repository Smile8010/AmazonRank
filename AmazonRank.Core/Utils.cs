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
using static System.String;

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


        /// <summary>
        /// 获取国家下拉框数据
        /// </summary>
        /// <returns></returns>
        public static List<CountryModel> GetCountryComboBoxData()
        {
            // 获取下拉框配置
            string countryKeyStr = GetConfigValue("Country.CfgKeys");
            var countryList = countryKeyStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<CountryModel> list = new List<CountryModel>();

            countryList.ForEach(key =>
            {
                string countryDataStr = GetConfigValue($"Country.{key}");
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

        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <param name="configKey"></param>
        /// <returns></returns>
        public static string GetConfigValue(string configKey, string defaultValue = "")
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
        public static T DeserializeObject<T>(string jsonStr) where T : class
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

        /// <summary>
        /// 获取默认 HttpClient
        /// </summary>
        /// <param name="isProxy"></param>
        /// <returns></returns>
        public static HttpClient GetDefaultHttpClient(bool isProxy = false)
        {
            string proxyIpAddress = GetConfigValue("Proxy.IPAddress");
            HttpClientHandler handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
            if (isProxy && !IsNullOrEmpty(proxyIpAddress))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyIpAddress);
            };
            HttpClient client = new HttpClient(handler);
            client.Timeout = new TimeSpan(0, 0, 1, 0);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            client.DefaultRequestHeaders.Add("User-Agent", GetConfigValue("Request.UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36"));

            return client;
        }

        /// <summary>
        /// 获取行数
        /// </summary>
        /// <param name="totalCount"></param>
        /// <param name="getFunc"></param>
        /// <returns></returns>
        public static List<string> GetLines(int totalCount, Func<int, string> getFunc = null)
        {
            List<string> lines = new List<string>();
            if (totalCount > 0 && getFunc != null)
            {
                for (int i = 0; i < totalCount; i++)
                {
                    string lineText = getFunc(i).Trim();
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
