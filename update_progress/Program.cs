using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using OAuth;
using System.Text.RegularExpressions;


namespace update_progress
{
    class Program
    {
        static readonly string _ConsumerKey = "a7s4VXb3W6tMxRKa8VwWg";
        static readonly string _ConsumerSecret = "nFXk4ignbCNr0HfhX5Er10XjiDF9CPfClCFBlFy05vY";

        // Access Token と Access Token Secret はそれぞれ確保してください
        static readonly string _AccessToken = "";
        static readonly string _AccessTokenSecret = "";

        static string _MyName { get; set; }
        static string _MyScreenName { get; set; }
        static double _CurrentProgress { get; set; }

        static void Main(string[] args)
        {
            GetMyInfo();

            StartUserStream();
        }

        static string BuildHeaderString(string EncodedUrl, string Method, string ExtString1, string ExtString2)
        {
            OAuthBase Oauth = new OAuthBase();
            string TimeStamp = Oauth.GenerateTimeStamp();
            string Nonce = Oauth.GenerateNonce();

            string SignatureBase = GenerateSignatureBase(EncodedUrl.Replace("%3a", "%3A").Replace("%2f", "%2F"), Method, TimeStamp, Nonce, ExtString1, ExtString2);

            string CompositeKey = _ConsumerSecret + "&" + _AccessTokenSecret;

            string Signature = Oauth.GenerateSignatureUsingHash(SignatureBase, new HMACSHA1(Encoding.UTF8.GetBytes(CompositeKey)));

            Signature = HttpUtility.UrlEncode(Signature);

            string HeaderString = "OAuth oauth_consumer_key=\"" + _ConsumerKey + "\", oauth_nonce=\"" + Nonce + "\", oauth_signature=\"" +
                Signature + "\", " + "oauth_signature_method=\"HMAC-SHA1\", oauth_timestamp=\"" + TimeStamp + "\", oauth_token=\"" +
                _AccessToken + "\", " + "oauth_version=\"1.0\"";

            return HeaderString;
        }

        static string GenerateSignatureBase(string EncodedUrl, string Method, string TimeStamp, string Nonce, string ExtString1, string ExtString2)
        {
            string SignatureBase = "";
            string _AND = Uri.EscapeDataString("&").ToString();
            string _EQ = Uri.EscapeDataString("=").ToString();

            SignatureBase += Method + "&" + EncodedUrl + "&";

            if (ExtString1 != "")
            {
                SignatureBase += Uri.EscapeDataString(ExtString1) + _AND;
            }

            SignatureBase += "oauth_consumer_key" + _EQ + _ConsumerKey + _AND + "oauth_nonce" + _EQ + Nonce + _AND + 
                "oauth_signature_method" + _EQ + "HMAC-SHA1" + _AND + "oauth_timestamp" + _EQ + TimeStamp + _AND + 
                "oauth_token" + _EQ + _AccessToken + _AND + "oauth_version" + _EQ + "1.0";

            if (ExtString2 != "")
            {
                SignatureBase += _AND + Uri.EscapeDataString(ExtString2);
            }

            return SignatureBase;
        }

        static void StartUserStream() { GetUserStream(); }

        static void GetUserStream()
        {
            while (true)
            {
                WebResponse Response;
                {
                    int i = 0;
                    do
                    {
                        string Url = "https://userstream.twitter.com/1.1/user.json";
                        string HeaderString = BuildHeaderString(HttpUtility.UrlEncode(Url), "GET", "", "");

                        HttpWebRequest Request = (HttpWebRequest)HttpWebRequest.Create(Url);
                        Request.Method = "GET";
                        Request.Headers.Add(HttpRequestHeader.Authorization, HeaderString);
                        Request.Timeout = Timeout.Infinite;
                        Request.ServicePoint.Expect100Continue = false;

                        Response = Request.GetResponse();

                        if (Response == null)
                        {
                            Thread.Sleep(2000 + (1000 * i));
                            i++;
                            if (i == 5)
                            {
                                Environment.Exit(-1);
                            }
                        }
                    } while (Response == null);
                }
                StreamReader Stream = new StreamReader(Response.GetResponseStream());

                while (true)
                {
                    try
                    {
                        string Text = Stream.ReadLine();
                        if (Text != null && Text.Length > 0)
                        {
                            var JsonRoot = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(Text);
                            if (JsonRoot.ContainsKey("user") && JsonRoot.ContainsKey("text"))
                            {

                                object TweetTextObj;
                                object TweetUserObj;
                                object TweetNameObj;
                                object TweetScreenNameObj;
                                var TweetUserSB = new StringBuilder();

                                JsonRoot.TryGetValue("user", out TweetUserObj);
                                var jrs = new JavaScriptSerializer();
                                jrs.Serialize(TweetUserObj, TweetUserSB);
                                var JsonUser = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(TweetUserSB.ToString(0, TweetUserSB.Length));
                                JsonUser.TryGetValue("name", out TweetNameObj);
                                JsonUser.TryGetValue("screen_name", out TweetScreenNameObj);

                                JsonRoot.TryGetValue("text", out TweetTextObj);
                                Console.WriteLine(TweetNameObj.ToString() + " @" + TweetScreenNameObj.ToString() + ":\n" + TweetTextObj.ToString() + "\n");

                                var task = ModeChanger(TweetScreenNameObj.ToString(), TweetTextObj.ToString());
                            }
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                    catch { break; }
                }
                try { Response.Close(); }
                catch { }
            }
        }
        
        static async Task ModeChanger(string user, string text)
        {
            if (text.Substring(0, 2) == "RT") return;

            Regex reg = new Regex("@" + _MyScreenName + " update_name (?<name>.*)");
            Match m = reg.Match(text);
            if (m.Success == true)
            {
                await Task.Run(() =>
                {
                    UpdateName(m.Groups["name"].Value);
                    Thread.Sleep(1000);
                    UpdateNameSendTweet(user, m.Groups["name"].Value);
                });
            }

            reg = new Regex(@"(?<name>.*)[(（]@" + _MyScreenName + "[)）]");
            m = reg.Match(text);
            if (m.Success == true)
            {
                await Task.Run(() => {
                    UpdateName(m.Groups["name"].Value);
                    Thread.Sleep(1000);
                    UpdateNameSendTweet(user, m.Groups["name"].Value);
                });
            }

            reg = new Regex("@" + _MyScreenName + " update_name_progress (?<name>.*)");
            m = reg.Match(text);
            if (m.Success == true)
            {
                await Task.Run(() =>
                {
                    UpdateName(m.Groups["name"].Value + ": " + _CurrentProgress.ToString() + "%");
                    Thread.Sleep(1000);
                    UpdateNameSendTweet(user, m.Groups["name"].Value + ": " + _CurrentProgress.ToString() + "%");
                });
            }

            reg = new Regex("@" + _MyScreenName + " update_progress (?<var>.*)");
            m = reg.Match(text);
            if (m.Success == true)
            {
                await Task.Run(() =>
                {
                    UpdateProgress(m.Groups["var"].Value);
                    Thread.Sleep(1000);
                    UpdateProgressSendTweet(user);
                });
            }

            return;
        }

        static void GetMyInfo()
        {
            string Url = "https://api.twitter.com/1.1/account/verify_credentials.json";
            string HeaderString = BuildHeaderString(HttpUtility.UrlEncode(Url), "GET", "", "");

            HttpWebRequest Request = (HttpWebRequest)HttpWebRequest.Create(Url);
            Request.Method = "GET";
            Request.Headers.Add(HttpRequestHeader.Authorization, HeaderString);

            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            StreamReader Stream = new StreamReader(Response.GetResponseStream());

            string Text = Stream.ReadLine();

            var JsonRoot = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(Text);
            object MyNameObj;
            object MyScreenNameObj;

            JsonRoot.TryGetValue("name", out MyNameObj);
            JsonRoot.TryGetValue("screen_name", out MyScreenNameObj);
            _MyName = MyNameObj.ToString();
            _MyScreenName = MyScreenNameObj.ToString();

            Regex reg = new Regex("(?<name>.*): (?<val>.*)%");
            Match m = reg.Match(_MyName);
            if (m.Success == true)
            {
                _MyName = m.Groups["name"].Value;

                double Progress;

                Double.TryParse(m.Groups["val"].Value, out Progress);
                _CurrentProgress = Progress;
            }
        }

        static void GetCurrentProgress()
        {
            Regex reg = new Regex("(?<name>.*): (?<val>.*)%");
            Match m = reg.Match(_MyName);
            if (m.Success == true)
            {
                _MyName = m.Groups["name"].Value;

                double Progress;

                Double.TryParse(m.Groups["val"].Value, out Progress);
                _CurrentProgress = Progress;
            }
            else
            {
                _CurrentProgress = 0;
            }
        }

        static void UpdateProgress(string Var)
        {
            if (Var == "++")
            {
                _CurrentProgress += 1;
            }
            else if (Var == "--")
            {
                _CurrentProgress -= 1;
            }
            else
            {
                double Progress;
                bool TryParse = Double.TryParse(Var, out Progress);

                if (TryParse == false) return;

                _CurrentProgress = Progress;
            }
            UpdateName(_MyName + ": " + _CurrentProgress.ToString() + "%");
        }

        static void UpdateProgressSendTweet(string user)
        {
            string TweetStr = ".@" + user + " が進捗を " + _CurrentProgress.ToString();
            TweetStr += "% に変更しました";

            if (TweetStr.Length > 140) return;
            SendTweet(TweetStr);
        }

        static void UpdateName(string Name)
        {
            if (Name.Length > 20) return;

            string Url = "https://api.twitter.com/1.1/account/update_profile.json";

            while (true)
            {
                try
                {
                    string HeaderString = BuildHeaderString(HttpUtility.UrlEncode(Url), "POST", "name=" + Uri.EscapeDataString(Name), "");
                    byte[] SendBytes = Encoding.UTF8.GetBytes("name=" + Uri.EscapeDataString(Name));

                    HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);
                    Request.Method = "POST";
                    Request.Headers.Add(HttpRequestHeader.Authorization, HeaderString);
                    Request.ContentType = "application/x-www-form-urlencoded";
                    Request.ContentLength = SendBytes.Length;
                    Request.ServicePoint.Expect100Continue = false;


                    Stream ReqStream = Request.GetRequestStream();
                    ReqStream.Write(SendBytes, 0, SendBytes.Length);
                    ReqStream.Close();
             
                    HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                    
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("[LOG] " + e + "\n");
                    Thread.Sleep(500);
                }
            }

            _MyName = Name;
            GetCurrentProgress();
        }

        static void UpdateNameSendTweet (string user, string name)
        {
            string TweetStr = ".@" + user + " が名前を " + name;
            TweetStr += " に変更しました";

            if (TweetStr.Length > 140) return;
            SendTweet(TweetStr);
        }

        static void SendTweet(string Str)
        {
            string Url = "https://api.twitter.com/1.1/statuses/update.json";

            while (true)
            {
                try
                {
                    string HeaderString = BuildHeaderString(HttpUtility.UrlEncode(Url), "POST", "", "status=" + Uri.EscapeDataString(Str));
                    byte[] SendBytes = Encoding.UTF8.GetBytes("status=" + Uri.EscapeDataString(Str));

                    HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);
                    Request.Method = "POST";
                    Request.Headers.Add(HttpRequestHeader.Authorization, HeaderString);
                    Request.ContentType = "application/x-www-form-urlencoded";
                    Request.ContentLength = SendBytes.Length;
                    Request.ServicePoint.Expect100Continue = false;


                    Stream ReqStream = Request.GetRequestStream();
                    ReqStream.Write(SendBytes, 0, SendBytes.Length);
                    ReqStream.Close();

                    HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("[LOG] " + e + "\n");
                    Str += "　";
                    if (Str.Length > 140)
                    {
                        Console.WriteLine("[LOG] 140文字を超えました\n");
                        break;
                    }
                    Thread.Sleep(500);
                }
            }
        }
    }
}
