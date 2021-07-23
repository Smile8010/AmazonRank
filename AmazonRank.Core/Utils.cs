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
using Polly;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Text.RegularExpressions;

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
        /// <param name="outputAction"></param>
        /// <returns></returns>
        public static async Task<Result<object>> InitQueryAsync(HttpClient client, string link, string zipCode, Action<string, bool> outputAction)
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

                // 获取设置 anti-csrftoken-a2z token
                var singleNode = documentResult.Data.DocumentNode.SelectSingleNode("//span[@id='nav-global-location-data-modal-action']");
                string anti_csrftoken_a2z = string.Empty;
                if (singleNode != null)
                {
                    var modelJson = singleNode.GetAttributeValue("data-a-modal", string.Empty);
                    if (!IsNullOrEmpty(modelJson))
                    {
                        var modelJobject = JsonConvert.DeserializeObject<JObject>(HttpUtility.HtmlDecode(modelJson));
                        if (modelJobject != null && modelJobject.ContainsKey("ajaxHeaders"))
                        {
                            string addressSelectionURL = modelJobject.Value<string>("url");
                            var ajaxHeaders = modelJobject["ajaxHeaders"];
                            anti_csrftoken_a2z = ajaxHeaders.Value<string>("anti-csrftoken-a2z");
                            HttpContent addressSelectionContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>());
                            addressSelectionContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                            addressSelectionContent.Headers.Add("anti-csrftoken-a2z", anti_csrftoken_a2z);
                            var setResponse = await client.PostAsync(link + addressSelectionURL, addressSelectionContent);
                            setResponse.EnsureSuccessStatusCode();
                            var responseText = await setResponse.Content.ReadAsStringAsync();
                            // <script type="text/javascript">P.now("GLUXWidget").execute(function(GLUXWidget){ if(GLUXWidget===undefined){P.declare("GLUXWidget", {COUNTRY_CODE : "US", COUNTRY_LIST_PLACEHOLDER : "Ship outside the US", CITY_LIST_PLACEHOLDER : "", WEBLABS : {"CART_PACKARD_LOCATION_DISPLAY_111103":"C","MULTI_LOCALE_CITIES_250112":"C","PACKARD_WEB_SPLIT_ZIPCODE_121888":"C","AUI_TYPERAMP_TARGETED_MOBILE_279980":"C","AB_GLOW_COMPANY_NAME_315982":"C","AUI_MM_DESKTOP_TARGETED_LAUNCH_291922":"C","AUI_TYPERAMP_MOBILE_265778":"C","AUI_MM_DESKTOP_TARGETED_EXP_291928":"C","PACKARD_GLOBAL_DESKTOP_124721":"T1"}, INITIAL_DISPLAY_ADDRESS_INDEX : 2, PCD_ENTRIES : [], GLOW_TITLE : "Choose your location", CONFIRM_HEADER : "You're now shopping for delivery to:", CSRF_TOKEN : "gGAqTApyd77ezOglPbCXkpuXdnR5Y2K8xRASkzcAAAAMAAAAAGD6hdZyYXcAAAAA", IDs:{"ADDRESS_LIST":"GLUXAddressList","ADDRESS_LIST_DELIVERY":"GLUXAddressListDelivery","ADDRESS_LIST_PICKUP":"GLUXAddressListPickup","ADDRESS_BLOCK":"GLUXAddressBlock","ADDESS_SELECTION_PLACEHOLDER":"GLUXAddressSelectionPlaceHolder","ADDRESS_SET_ERROR":"GLUXAddressSetError","ADDRESS_SUCCESS_PLACEHOLDER":"GLUXHiddenSuccessSelectedAddressPlaceholder","CHANGE_POSTAL_CODE_LINK":"GLUXChangePostalCodeLink","CONFIRM_CLOSE":"GLUXConfirmClose","CONFIRM_CLOSE_LABEL":"GLUXConfirmClose-announce","COUNTRY_HIDDEN_DIV":"GLUXHiddenCountryDiv","COUNTRY_LIST":"GLUXCountryList"

                            //string pattern = @"of\s(?<num>[\w\W]+)\sresults";
                            //var m = Regex.Match(resultNumNode.InnerText, pattern);

                            //string numStr = m.Groups["num"].Value;
                            //if (!IsNullOrEmpty(numStr))
                            //{
                            //    sModel.ResultNumString = numStr;
                            //}

                            string pattern =@"CSRF_TOKEN[^""]+""(?<token>[^""]*?)""";
                            var m = Regex.Match(responseText, pattern);
                            if (m.Success)
                                anti_csrftoken_a2z = m.Groups["token"].Value;
                        }
                    }
                }

                if (IsNullOrEmpty(anti_csrftoken_a2z))
                {
                    outputAction($"设置收货目的地：{zipCode} 失败,无法获取 请求token ", false);
                }
                else
                {
                    bool isValid = await Policy.HandleResult<bool>(o => !o).WaitAndRetryAsync(3, o => TimeSpan.FromMilliseconds(100))
                        .ExecuteAsync(() => addressChangeAsync(client, link, zipCode, anti_csrftoken_a2z));

                    if (!isValid)
                    {
                        outputAction($"设置收货目的地：{zipCode} 失败", false);
                    }
                }

                return Result<object>.OK();

            }
            catch (Exception ex)
            {
                return Result<object>.Error($"初始化查询异常：{ex.Message}");
            }
        }

        static async Task<bool> addressChangeAsync(HttpClient client, string link, string zipCode, string anti_csrftoken_a2z)
        {
            List<KeyValuePair<string, string>> paramsList = new List<KeyValuePair<string, string>>() {
                    new KeyValuePair<string, string>("locationType","LOCATION_INPUT"),
                    new KeyValuePair<string, string>("zipCode",zipCode),
                    new KeyValuePair<string, string>("storeContext","generic"),
                    new KeyValuePair<string, string>("deviceType","web"),
                    new KeyValuePair<string, string>("pageType","Gateway"),
                    new KeyValuePair<string, string>("actionSource","glow"),
                    new KeyValuePair<string, string>("almBrandId","undefined"),
                };

            HttpContent content = new FormUrlEncodedContent(paramsList);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            content.Headers.Add("anti-csrftoken-a2z", anti_csrftoken_a2z);
            var setResponse = await client.PostAsync($"{link}/gp/delivery/ajax/address-change.html", content);
            setResponse.EnsureSuccessStatusCode();
            var responseText = await setResponse.Content.ReadAsStringAsync();

            var data = new { isValidAddress = 0 };

            data = JsonConvert.DeserializeAnonymousType(responseText, data);
            if (data == null || data.isValidAddress != 1)
            {
                return false;
            }
            return true;
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
            //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            client.DefaultRequestHeaders.Add("User-Agent", GetRandomUserAgent());
            client.DefaultRequestHeaders.Connection.Add("keep-alive");
            return client;
        }

        /// <summary>
        /// 获取随机User-Agent
        /// </summary>
        /// <returns></returns>
        public static string GetRandomUserAgent()
        {
            var randomValue = GetConfigValue("Request.UserAgent.Random");
            if (IsNullOrEmpty(randomValue))
            {
                return GetConfigValue("Request.UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
            }
            return randomValue.Replace("{{RandomValue}}", new Random().Next(60, 76).ToString());
        }

        /// <summary>
        /// 刷新User-Agent
        /// </summary>
        /// <param name="client"></param>
        public static void RefreshRandomUserAgent(HttpClient client)
        {
            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", GetRandomUserAgent());
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

        private static Dictionary<string, HttpClient> _cacheHttpClient = new Dictionary<string, HttpClient>();

        /// <summary>
        /// 获取缓存的 HttpClient
        /// </summary>
        /// <param name="countryModel"></param>
        /// <returns></returns>
        public static HttpClient GetCacheClient(CountryModel countryModel, out bool isCreated)
        {
            HttpClient client;
            string cacheKey = countryModel.Link;
            isCreated = false;
            if (!_cacheHttpClient.TryGetValue(cacheKey, out client))
            {
                client = GetDefaultHttpClient(countryModel.isProxy);
                _cacheHttpClient.Add(cacheKey, client);
                isCreated = true;
            }
            return client;
        }

        /// <summary>
        /// 清空加载器缓存
        /// </summary>
        public static void ClearClientCache()
        {
            var cacheKeyList = _cacheHttpClient.Keys.ToList();
            foreach (var item in cacheKeyList)
            {
                _cacheHttpClient[item].Dispose();
                _cacheHttpClient.Remove(item);
            }
        }

        /// <summary>
        /// 获取已经初始化的 加载器
        /// </summary>
        /// <param name="countryModel">下拉框选择记录</param>
        /// <param name="outputAction">用于输出消息的方法</param>
        /// <returns></returns>
        public async static Task<Result<HttpClient>> GetInitCacheClientAsync(CountryModel countryModel, Action<string, bool> outputAction)
        {
            var client = GetCacheClient(countryModel, out bool isCreated);
            if (outputAction == null)
            {
                outputAction = (s, b) => { };
            }
            if (isCreated)
            {
                outputAction($"初始化加载器...", true);
                Result<object> initResult = await Utils.InitQueryAsync(client, countryModel.Link, countryModel.ZipCode, outputAction);
                if (!initResult.Success)
                {
                    outputAction(initResult.Msg, false);
                    return Result<HttpClient>.Error("");
                }
            }
            else
            {
                outputAction($"获取缓存的加载器...", true);
            }
            return Result<HttpClient>.OK(client);
        }
    }
}
