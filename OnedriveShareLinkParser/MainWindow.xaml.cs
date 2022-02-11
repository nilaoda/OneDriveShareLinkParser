using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static OnedriveShareLinkParser.Util;
using MessageBox = System.Windows.MessageBox;

namespace OnedriveShareLinkParser
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        static string DemoLink = "https://gitaccuacnz2-my.sharepoint.com/:f:/g/personal/mail_finderacg_com/EheQwACFhe9JuGUn4hlg9esBsKyk5jp9-Iz69kqzLLF5Xw?e=FG7SHh";
        static int Limit = 0;

        CookieContainer cookieContainer = new CookieContainer();
        List<ODItem> onedriveItems = new List<ODItem>();


        public MainWindow()
        {
            InitializeComponent();
            Txt_Link.Text = DemoLink;
            Data_Output.ItemsSource = onedriveItems;
            Txt_SavePath.Text = Environment.CurrentDirectory;
        }

        private async void Btn_Parse_Click(object sender, RoutedEventArgs e)
        {
            Btn_Parse.IsEnabled = false;
            try
            {
                Limit = Convert.ToInt32(Txt_Limit.Text);
                onedriveItems.Clear();
                Data_Output.ItemsSource = onedriveItems;
                cookieContainer = new CookieContainer();
                var url = Txt_Link.Text;
                onedriveItems = await GetFilsAsync(url);
                if (Limit > 0 && onedriveItems.Count > Limit)
                {
                    onedriveItems = onedriveItems.Take(Limit).ToList();
                }
                foreach (var item in onedriveItems)
                {
                    Console.WriteLine(item);
                }
                Console.WriteLine(onedriveItems.Count);
                Txt_Cookie.Text = GetCookieString(url, cookieContainer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Btn_Parse.IsEnabled = true;
            }
            Data_Output.ItemsSource = onedriveItems;
            Btn_Parse.IsEnabled = true;
        }

        private string GetCookieString(string url, CookieContainer cookieContainer)
        {
            var uri = ParseUri(url);
            var cookies = new List<string>();
            foreach (var cookie in cookieContainer.GetCookies(new Uri($"{uri.Scheme}://{uri.Host}")))
            {
                cookies.Add(cookie.ToString());
            }
            return string.Join(";", cookies);
        }

        async Task<List<ODItem>> GetFilsAsync(string url)
        {
            var list = new List<ODItem>();
            var jsonArray = new JArray();
            bool isSharepoint = false;
            if (!url.Contains("-my"))
                isSharepoint = true;
            var resp = await DoGetAsync(url, cookieContainer);
            if (resp.StatusCode == HttpStatusCode.Found || resp.StatusCode == HttpStatusCode.Moved)
            {
                url = resp.Headers.Location.AbsoluteUri;
                resp = await DoGetAsync(url, cookieContainer);
            }
            var respTxt = await resp.Content.ReadAsStringAsync();
            if (!respTxt.Contains(",\"FirstRow\""))
                throw new Exception("这个文件夹没有文件");
            var rootFolder = System.Web.HttpUtility.UrlDecode(GetQueryString("id", url));
            var redirectSplitURL = url.Split('/');
            var relativeFolder = "";
            var downloadPrefix = string.Join("/", redirectSplitURL.Take(redirectSplitURL.Length - 1)) + "/download.aspx?UniqueId=";
            if (isSharepoint)
            {
                var pat = Regex.Match(respTxt, "templateUrl\":\"(.*?)\"").Groups[1].Value;
                Console.WriteLine(pat);
                Console.ReadLine();
                var uri = ParseUri(url);
                var arr = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}".Split('/');
                downloadPrefix = string.Join("/", arr.Take(arr.Length - 1)) + "/download.aspx?UniqueId=";
            }
            //获取相对路径
            foreach (var i in rootFolder.Split('/'))
            {
                if (isSharepoint)
                {
                    if (i != "Shared Documents")
                    {
                        relativeFolder += i + "/";
                    }
                    else
                    {
                        relativeFolder += i;
                        break;
                    }
                }
                else
                {
                    if (i != "Documents")
                    {
                        relativeFolder += i + "/";
                    }
                    else
                    {
                        relativeFolder += i;
                        break;
                    }
                }
            }
            var relativeUrl = System.Web.HttpUtility.UrlEncode(relativeFolder).Replace("_", "%5f").Replace("-", "%2d").ToUpper();
            var rootFolderUrl = System.Web.HttpUtility.UrlEncode(rootFolder).Replace("_", "%5f").Replace("-", "%2d").ToUpper();
            var graphql = "{\"query\":\"query (\n" +
                "        $listServerRelativeUrl: String!,$renderListDataAsStreamParameters: RenderListDataAsStreamParameters!,$renderListDataAsStreamQueryString: String!\n" +
                "        )\n" +
                "      {\n" +
                "      \n" +
                "      legacy {\n" +
                "      \n" +
                "      renderListDataAsStream(\n" +
                "      listServerRelativeUrl: $listServerRelativeUrl,\n" +
                "      parameters: $renderListDataAsStreamParameters,\n" +
                "      queryString: $renderListDataAsStreamQueryString\n" +
                "      )\n" +
                "    }\n" +
                "      \n" +
                "      \n" +
                "  perf {\n" +
                "    executionTime\n" +
                "    overheadTime\n" +
                "    parsingTime\n" +
                "    queryCount\n" +
                "    validationTime\n" +
                "    resolvers {\n" +
                "      name\n" +
                "      queryCount\n" +
                "      resolveTime\n" +
                "      waitTime\n" +
                "    }\n" +
                "  }\n" +
                "    }\",\"variables\":{\"listServerRelativeUrl\":\"" + relativeFolder + "\",\"renderListDataAsStreamParameters\":{\"renderOptions\":5707527,\"allowMultipleValueFilterForTaxonomyFields\":true,\"addRequiredFields\":true,\"folderServerRelativeUrl\":\"" + rootFolder + "\"},\"renderListDataAsStreamQueryString\":\"@a1=\'" + relativeUrl + "\'&RootFolder=" + rootFolderUrl + "&TryNewExperienceSingle=TRUE\"}}";

            var headers = new Dictionary<string, string>()
            {
                ["referer"] = url,
                ["authority"] = ParseUri(url).Host,
            };

            var apiUrl = string.Join("/", redirectSplitURL.Take(redirectSplitURL.Length - 3)) + "/_api/v2.1/graphql";
            var graphqlRespTxt = await DoPostForStringAsync(apiUrl, cookieContainer, graphql, headers);
            var json = JObject.Parse(graphqlRespTxt);
            foreach (var item in json["data"]["legacy"]["renderListDataAsStream"]["ListData"]["Row"].Value<JArray>())
            {
                jsonArray.Add(item);
            }
            //处理多页逻辑
            if (json["data"]["legacy"]["renderListDataAsStream"]["ListData"].Value<JObject>().ContainsKey("NextHref"))
            {
                var nextHref = json["data"]["legacy"]["renderListDataAsStream"]["ListData"]["NextHref"].ToString()
                    + $"&@a1=%27{relativeUrl}%27&TryNewExperienceSingle=TRUE";
                var listViewXml = json["data"]["legacy"]["renderListDataAsStream"]["ViewMetadata"]["ListViewXml"].ToString();
                var renderListDataAsStreamPara = "{\"parameters\":{\"__metadata\":{\"type\":\"SP.RenderListDataParameters\"},\"RenderOptions\":1216519,\"ViewXml\":\"" + listViewXml.Replace("\"", "\\\"") + "\",\"AllowMultipleValueFilterForTaxonomyFields\":true,\"AddRequiredFields\":true}}";
                apiUrl = string.Join("/", redirectSplitURL.Take(redirectSplitURL.Length - 3)) + "/_api/web/GetListUsingPath(DecodedUrl=@a1)/RenderListDataAsStream" + nextHref;
                graphqlRespTxt = await DoPostForStringAsync(apiUrl, cookieContainer, renderListDataAsStreamPara, headers);
                json = JObject.Parse(graphqlRespTxt);
                while (json["ListData"].Value<JObject>().ContainsKey("NextHref"))
                {
                    nextHref = json["ListData"]["NextHref"].ToString()
                        + $"&@a1=%27{relativeUrl}%27&TryNewExperienceSingle=TRUE";
                    foreach (var item in json["ListData"]["Row"].Value<JArray>())
                    {
                        jsonArray.Add(item);
                    }
                    apiUrl = string.Join("/", redirectSplitURL.Take(redirectSplitURL.Length - 3)) + "/_api/web/GetListUsingPath(DecodedUrl=@a1)/RenderListDataAsStream" + nextHref;
                    graphqlRespTxt = await DoPostForStringAsync(apiUrl, cookieContainer, renderListDataAsStreamPara, headers);
                    json = JObject.Parse(graphqlRespTxt);
                }
                foreach (var item in json["ListData"]["Row"].Value<JArray>())
                {
                    jsonArray.Add(item);
                }
            }

            foreach (var item in jsonArray)
            {
                if (Limit > 0 && list.Count > Limit)
                {
                    return list;
                }
                //进入文件夹
                if (item["FSObjType"].ToString() == "1")
                {
                    var name = item["FileLeafRef"].ToString();
                    var path = item["FileRef"].ToString();
                    var id = item["UniqueId"].ToString();
                    Console.WriteLine($"文件夹：{name}, ID：{id}");
                    url = url.Replace(GetQueryString("id", url), System.Web.HttpUtility.UrlEncode(path).Replace("_", "%5f").Replace("-", "%2d"));
                    list.AddRange(await GetFilsAsync(url));
                }
                else
                {
                    var name = item["FileLeafRef"].ToString();
                    var id = item["UniqueId"].ToString();
                    var time = DateTime.Parse(item["Modified."].ToString());
                    var size = Convert.ToInt64(item["SMTotalSize"].ToString());
                    var path = item["FileRef"].ToString();
                    var odItem = new ODItem()
                    {
                        Name = name,
                        Id = id,
                        Size = FormatFileSize(size),
                        ModifiedTime = time.ToString("yyyy-MM-dd HH:mm:ss"),
                        Path = System.IO.Path.GetDirectoryName(path.Substring(path.IndexOf("Documents/") + 10)),
                        DownloadLink = downloadPrefix + id.TrimStart('{').TrimEnd('}')
                    };
                    if (!list.Contains(odItem))
                    {
                        list.Add(odItem);
                    }
                }
            }
            return list;
        }

        private void Btn_SelectSavePath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFileDialog = new FolderBrowserDialog();  //选择文件夹
            openFileDialog.Description = "选择保存路径";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Txt_SavePath.Text = openFileDialog.SelectedPath;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = Data_Output.SelectedItems;
            try
            {
                if (selected != null && selected.Count > 0)
                {
                    MessageBoxResult confirm = MessageBox.Show($"确认要推送这[{selected.Count}]项吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm == MessageBoxResult.Yes)
                    {
                        foreach (var i in selected)
                        {
                            var item = (ODItem)i;
                            PushToIDM(item.DownloadLink, item.Name, System.IO.Path.Combine(Txt_SavePath.Text, item.Path), Txt_Cookie.Text);
                        }
                        MessageBox.Show("完成！");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (onedriveItems.Count > 0)
                {
                    MessageBoxResult confirm = MessageBox.Show($"确认要推送这[{onedriveItems.Count}]项吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm == MessageBoxResult.Yes)
                    {
                        foreach (var item in onedriveItems)
                        {
                            PushToIDM(item.DownloadLink, item.Name, System.IO.Path.Combine(Txt_SavePath.Text, item.Path), Txt_Cookie.Text);
                        }
                        MessageBox.Show("完成！");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void Btn_RenewCookie_Click(object sender, RoutedEventArgs e)
        {
            Btn_RenewCookie.IsEnabled = false;
            try
            {
                cookieContainer = new CookieContainer();
                await DoGetAsync(Txt_Link.Text, cookieContainer);
                Txt_Cookie.Text = GetCookieString(Txt_Link.Text, cookieContainer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Btn_RenewCookie.IsEnabled = true;
            }
            Btn_RenewCookie.IsEnabled = true;
        }
    }
}
