/*
 * Web2Mail Version 1.1
 * 
 * Copyright 2014 kemunpus@hotmail.com.
 * 
 * Project Page:
 * https://kemunpus.visualstudio.com/DefaultCollection/KemunpusProject
 * 
 * See also:
 * http://kemunpus.azurewebsites.net/
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kemunpus.Web2Mail.Common {

    public sealed class WebClient : IDisposable {

        private static readonly Dictionary<string, string> CharSetDictionary = new Dictionary<string, string>() {
            {"windows-31j","shift_jis"}
        };

        private static readonly string[] NoSchemeRegexs = {
            "(\\s+src\\s*=\\s*[\"'])(//)",
            "(\\s+href\\s*=\\s*[\"'])(//)",
            "(:\\s*url\\(\\s*)(//)"
        };

        private static readonly string[] NoHostUrlRegexs = {
            "(\\s+src\\s*=\\s*[\"'])(/[\\w\\.])",
            "(\\s+href\\s*=\\s*[\"'])(/[\\w\\.])",
            "(:\\*surl\\(\\s*)(/[\\w\\.])"
        };

        private static readonly string[] NoBaseUrlRegexs = {
            "(\\s+src\\s*=\\s*[\"'])(?!(http://|https://|.*data:image/))",
            "(\\s+href\\s*=\\s*[\"'])(?!(http|https)://)",
            "(:\\s*url\\(\\s*)(?!(http|https)://)",
        };

        private const string MyMark = "<!-- @WEB2MAIL@ -->";

        private readonly Encoding HttpCharSet;
        private readonly HttpClientHandler HttpClientHandler;
        private readonly HttpClient HttpClient;
        private readonly int HtmlBufferSize;

        public WebClient(Encoding httpCharSet, int maxAutomaticRedirections, int httpBufferSize, int timeout, int htmlBufferSize, string userAgent, string acceptLanguage) {
            HttpCharSet = httpCharSet;
            HtmlBufferSize = htmlBufferSize;

            HttpClientHandler = new HttpClientHandler();

            HttpClientHandler.MaxAutomaticRedirections = Math.Max(maxAutomaticRedirections, 1);
            HttpClientHandler.AllowAutoRedirect = maxAutomaticRedirections > 0 ? true : false;
            HttpClientHandler.UseCookies = false;
            HttpClientHandler.UseDefaultCredentials = false;
            HttpClientHandler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;

            HttpClient = new HttpClient(HttpClientHandler);

            HttpClient.MaxResponseContentBufferSize = (long)httpBufferSize;
            HttpClient.Timeout = System.TimeSpan.FromSeconds(timeout);

            if (httpCharSet != null) {
                HttpClient.DefaultRequestHeaders.Add("Accept-Charset", httpCharSet.WebName);
            }

            if (!string.IsNullOrEmpty(userAgent)) {
                HttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }

            if (!string.IsNullOrEmpty(acceptLanguage)) {
                HttpClient.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
            }
        }

        public void Dispose() {

            if (HttpClient != null) {
                HttpClient.CancelPendingRequests();
                HttpClient.Dispose();
            }

            if (HttpClientHandler != null) {
                HttpClientHandler.Dispose();
            }
        }

        public async Task<string> LoadHtmlAsync(Uri uri) {
            Debug.Assert(uri != null);

            string html = string.Empty;

            Debug.WriteLine("WebClient : loading html from [{0}].", uri);

            using (HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync(uri))
            using (HttpContent httpContent = httpResponseMessage.Content) {
                httpResponseMessage.EnsureSuccessStatusCode();

                if (httpContent.Headers.ContentType != null && httpContent.Headers.ContentType.MediaType.StartsWith("text/html")) {
                    byte[] byteBuffer = await httpContent.ReadAsByteArrayAsync();

                    html = DetermineCharSet(httpContent, byteBuffer).GetString(byteBuffer, 0, byteBuffer.Length);

                    if (html.Length >= HtmlBufferSize) {
                        throw new IOException(AppUtils.GetResourceString("Message/HtmlBufferError"));
                    }
                }
            }

            return html;
        }

        public string NormalizeLink(string html, Uri uri) {
            Debug.Assert(html != null);

            if (uri != null) {
                string hostUrl = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);

                string noSchemeLink = string.Format(CultureInfo.CurrentCulture, "$1{0}:$2", uri.GetComponents(UriComponents.Scheme, UriFormat.Unescaped));

                foreach (string regexString in NoSchemeRegexs) {
                    html = new Regex(regexString, RegexOptions.IgnoreCase | RegexOptions.Singleline).Replace(html, noSchemeLink);
                }

                string noHostUrlLink = string.Format(CultureInfo.CurrentCulture, "$1{0}$2", hostUrl);

                foreach (string regexString in NoHostUrlRegexs) {
                    html = new Regex(regexString, RegexOptions.IgnoreCase | RegexOptions.Singleline).Replace(html, noHostUrlLink);
                }

                string baseUri = string.Format(CultureInfo.CurrentCulture, "{0}/{1}", hostUrl, uri.GetComponents(UriComponents.Path, UriFormat.Unescaped));

                int firstSlash = baseUri.IndexOf('/');
                int lastSlash = baseUri.LastIndexOf('/');

                if (lastSlash > firstSlash) {
                    baseUri = baseUri.Substring(0, lastSlash + 1);
                }

                string noBaseUrlLink = string.Format(CultureInfo.CurrentCulture, "$1{0}", baseUri);

                foreach (string regexString in NoBaseUrlRegexs) {
                    html = new Regex(regexString, RegexOptions.IgnoreCase | RegexOptions.Singleline).Replace(html, noBaseUrlLink);
                }

                if (html.Length >= HtmlBufferSize) {
                    throw new IOException(AppUtils.GetResourceString("Message/HtmlBufferError"));
                }
            }

            return html;
        }

        public string RemoveScript(string html) {
            Debug.Assert(html != null);

            html = new Regex("<noscript>|</noscript>", RegexOptions.IgnoreCase | RegexOptions.Singleline).Replace(html, MyMark);
            html = new Regex("<\\s?script.*?(/\\s?>|<\\s?/\\s?script\\s?>)", RegexOptions.IgnoreCase | RegexOptions.Singleline).Replace(html, MyMark);

            Regex scriptCloseRegex = new Regex("</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            Match match;

            while ((match = scriptCloseRegex.Match(html)).Success) {
                int myMarkIndex = html.LastIndexOf(MyMark, match.Index, StringComparison.CurrentCulture);

                if (myMarkIndex < 0) {
                    break;
                }

                html = html.Remove(myMarkIndex, (match.Index - myMarkIndex) + match.Length);
            }

            return new Regex(MyMark).Replace(html, string.Empty);
        }

        public string RemoveNop(string html) {
            Debug.Assert(html != null);

            return new Regex("^[\\s\\n\\r]+", RegexOptions.Multiline).Replace(html, string.Empty).Trim();
        }

        public async Task<string> ConvertLinkToAttachmentAsync(string html) {
            Debug.Assert(html != null);

            Regex srcRegex = new Regex("\\s+src\\s*=\\s*[\"']((http|https)://[a-zA-Z0-9\\-_\\./?%&=]*)[\"']", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            StringBuilder buffer = new StringBuilder((int)HttpClient.MaxResponseContentBufferSize);

            int index = 0;

            while (true) {

                if (buffer.Length >= HtmlBufferSize) {
                    throw new IOException(AppUtils.GetResourceString("Message/HtmlBufferError"));
                }

                Match match = srcRegex.Match(html, index);

                if (!match.Success) {
                    buffer.Append(html.Substring(index));

                    break;
                }

                buffer.Append(html.Substring(index, match.Index - index));

                index = match.Index + match.Length;

                string url = match.Groups[1].Value;

                Debug.WriteLine("WebClient : loading data from [{0}].", url);

                using (HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync(url))
                using (HttpContent httpContent = httpResponseMessage.Content) {
                    httpResponseMessage.EnsureSuccessStatusCode();

                    if (httpContent.Headers != null && httpContent.Headers.ContentType != null) {
                        buffer.Append(" src=\"data:");
                        buffer.Append(httpContent.Headers.ContentType);
                        buffer.Append(";base64,");
                        buffer.Append(Convert.ToBase64String(await httpContent.ReadAsByteArrayAsync()));
                        buffer.Append('\"');

                    } else {
                        buffer.Append(" src=\"");
                        buffer.Append(url); // use url instead...
                        buffer.Append('\"');
                    }
                }
            }

            return buffer.ToString();
        }

        private Encoding DetermineCharSet(HttpContent httpContent, byte[] byteBuffer) {
            Debug.Assert(httpContent != null);
            Debug.Assert(byteBuffer != null);

            try {

                if (httpContent.Headers != null && httpContent.Headers.ContentType != null) {
                    string headerCharSet = httpContent.Headers.ContentType.CharSet;

                    if (!string.IsNullOrEmpty(headerCharSet)) {
                        return Encoding.GetEncoding(headerCharSet);
                    }
                }

            } catch (ArgumentException e) {
                Debug.WriteLine("WebClient : exception [{0}] has occurred.", e.ToString());
            }

            try {
                int checkLength = Math.Min(1024, byteBuffer.Length);

                Match match = new Regex("(charset\\s*=\\s*[\"']*|encoding\\s*=\\s*[\"']*)([a-zA-Z0-9_\\-]+)(\\s*[\"']*)", RegexOptions.IgnoreCase | RegexOptions.Singleline).Match(HttpCharSet.GetString(byteBuffer, 0, checkLength));

                if (match.Success && match.Groups.Count >= 3) {
                    string detectedCharSet = match.Groups[2].Value.ToLower();
                    string correctCharSet;

                    if (CharSetDictionary.TryGetValue(detectedCharSet, out correctCharSet)) {
                        detectedCharSet = correctCharSet;
                    }

                    return Encoding.GetEncoding(detectedCharSet);
                }

            } catch (ArgumentException e) {
                Debug.WriteLine("WebClient : exception [{0}] has occurred.", e.ToString());
            }

            return HttpCharSet;
        }
    }
}