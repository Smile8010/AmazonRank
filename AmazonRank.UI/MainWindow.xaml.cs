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
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace AmazonRank.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        HttpClient client = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip });


        public MainWindow()
        {
            InitializeComponent();
            //client.BaseAddress = new Uri("https://www.amazon.com");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            this.Output.Document.Blocks.Clear();
            this.Link.Text = "https://www.amazon.com/?language=en_US";
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //https://www.amazon.com/s?k=women+bag&page=2

        }

        private async void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            string link = this.Link.Text;
            string output = await client.GetStringAsync(link);

            //RefreshOutput(output);
            HtmlDocument document = new HtmlDocument();

            await Task.Run(() => document.LoadHtml(output));

            var node = await Task.Run(() => document.DocumentNode.SelectNodes("//*[@id='glow-ingress-line2']").FirstOrDefault());
            RefreshOutput(node.InnerText);
            if (node != null && !node.InnerText.Contains("New York"))
            {
                //client.DefaultRequestHeaders.Referrer = new Uri("https://www.amazon.com/?language=en_US");
                //client.DefaultRequestHeaders
                List<KeyValuePair<string, string>> paramsList = new List<KeyValuePair<string, string>>() {
                    new KeyValuePair<string, string>("locationType","LOCATION_INPUT"),
                    new KeyValuePair<string, string>("zipCode","10041"),
                    new KeyValuePair<string, string>("storeContext","generic"),
                    new KeyValuePair<string, string>("deviceType","web"),
                    new KeyValuePair<string, string>("pageType","Gateway"),
                    new KeyValuePair<string, string>("actionSource","glow")
                };



                

               //var setResponse = await client.PostAsync("/gp/delivery/ajax/address-change.html", new StringContent(JsonConvert.SerializeObject(paramsList), Encoding.UTF8, "text/json"));
               var setResponse = await client.PostAsync("https://www.amazon.com/gp/delivery/ajax/address-change.html", new FormUrlEncodedContent(paramsList));

                RefreshOutput(setResponse.IsSuccessStatusCode.ToString() + await setResponse.Content.ReadAsStringAsync() );
            }
            else
            {
                RefreshOutput(node.InnerText);
            }

            //var node = document.DocumentNode.ChildNodes.AsQueryable().Where(o => o.Id == "nav-packard-glow-loc-icon").FirstOrDefault();

            //nav-packard-glow-loc-icon
            //document.DocumentNode.ChildNodes.AsQueryable().Where()
        }

        private void RefreshOutput(string text)
        {
            this.Output.Document.Blocks.Clear();
            this.Output.AppendText(text);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string output = await client.GetStringAsync("https://www.amazon.com/s?k=women+bag");
            HtmlDocument document = new HtmlDocument();

            await Task.Run(() => document.LoadHtml(output));

            ////*[@id="search"]/div[1]/div[2]/div/span[3]/div[1]
            var nodes = await Task.Run(() => document.DocumentNode.SelectNodes("//*[@id='search']/div[1]/div[2]/div/span[3]/div[1]/div"));

            StringBuilder outputS = new StringBuilder();
            nodes.ToList().ForEach(l =>
            {
                outputS.AppendLine(l.Attributes["data-asin"].Value);
            });
            RefreshOutput(outputS.ToString());
        }
    }
}
