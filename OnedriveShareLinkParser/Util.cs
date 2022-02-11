using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OnedriveShareLinkParser
{
    class Util
    {

        public static async Task<HttpResponseMessage> DoGetAsync(string url, CookieContainer cookieContainer, Dictionary<string, string> headers = null)
        {
            HttpResponseMessage resp = null;
            try
            {
                using (HttpClient AppHttpClient = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.GZip,
                    CookieContainer = cookieContainer
                })
                {
                    Timeout = TimeSpan.FromMinutes(5)
                })
                {
                    using (var webRequest = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        webRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0 Safari/605.1.15");
                        webRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
                        webRequest.Headers.CacheControl = CacheControlHeaderValue.Parse("no-cache");
                        webRequest.Headers.Connection.Clear();
                        if (headers != null)
                        {
                            foreach (var h in headers)
                            {
                                webRequest.Headers.TryAddWithoutValidation(h.Key, h.Value);
                            }
                        }
                        resp = await AppHttpClient.SendAsync(webRequest, HttpCompletionOption.ResponseHeadersRead);
                        //htmlCode = await webResponse.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception)
            {
                ;
            }
            return resp;
        }

        public static async Task<string> DoPostForStringAsync(string Url, CookieContainer cookieContainer, string postData, Dictionary<string, string> headers = null)
        {
            Console.WriteLine(Url);
            string htmlCode = string.Empty;
            using (HttpClient AppHttpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip,
                CookieContainer = cookieContainer
            })
            {
                Timeout = TimeSpan.FromMinutes(5)
            })
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Url))
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0 Safari/605.1.15");
                    if (headers != null)
                    {
                        foreach (var h in headers)
                        {
                            request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                        }
                    }
                    request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(postData));
                    //Add entity header
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json;odata=verbose");
                    var webResponse = await AppHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    Stream myRequestStream = await webResponse.Content.ReadAsStreamAsync();
                    htmlCode = await webResponse.Content.ReadAsStringAsync();
                }
            }
            return htmlCode;
        }

        public static Uri ParseUri(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                return uri;
            }
            return null;
        }

        public static string FormatFileSize(double fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00} GB", ((double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00} MB", ((double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00} KB", ((double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0} bytes", fileSize);
            }
        }

        /// <summary>    
        /// 获取url字符串参数，返回参数值字符串    
        /// </summary>    
        /// <param name="name">参数名称</param>    
        /// <param name="url">url字符串</param>    
        /// <returns></returns>    
        public static string GetQueryString(string name, string url)
        {
            Regex re = new Regex(@"(^|&)?(\w+)=([^&]+)(&|$)?", System.Text.RegularExpressions.RegexOptions.Compiled);
            MatchCollection mc = re.Matches(url);
            foreach (Match m in mc)
            {
                if (m.Result("$2").Equals(name))
                {
                    return m.Result("$3");
                }
            }
            return "";
        }

        public static void PushToIDM(string url, string name, string path, string cookie)
        {
            try
            {
                new IDManTypeInfo.CIDMLinkTransmitterClass().SendLinkToIDM2(
                    url, //URL
                    "", //Referer
                    cookie, //Cookies
                    "", //Data
                    "", //Username
                    "", //Userpassword
                    path, //LocalPath
                    name,  //LocalFileName
                    2, //Flag
                    "",
                    null
                    );
            }
            catch (Exception)
            {
                throw new Exception("IDM调用失败，请检查是否安装的IDM!");
            }
        }
    }
}
