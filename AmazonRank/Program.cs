using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AmazonRank
{
    class Program
    {
        static HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //string htmlStr = client.GetStringAsync("https://www.amazon.com/errors/validateCaptcha?amzn=rhYfolUtTd6OBqtWs3u1eA==&amzn-r=&#047;&field-keywords=EGHNNN").Result;
            string htmlStr = client.GetStringAsync("https://www.amazon.com").Result;
            File.WriteAllText("D://t1.txt", htmlStr,Encoding.UTF8);

            Console.ReadKey();
        }
    }
}
