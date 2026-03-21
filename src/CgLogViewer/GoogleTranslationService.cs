using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace CgLogViewer
{
    public sealed class GoogleTranslationService : ITranslationService, IDisposable
    {
        readonly HttpClient httpClient = new HttpClient();
        readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public GoogleTranslationService()
        {
            httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public TranslationProvider Provider => TranslationProvider.Google;

        public async Task<string> TranslateToJapaneseAsync(string authKey, string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                throw new InvalidOperationException("Google Cloud Translation API キーが設定されていません。");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string url = $"https://translation.googleapis.com/language/translate/v2?key={HttpUtility.UrlEncode(authKey.Trim())}";
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("q", text),
                    new KeyValuePair<string, string>("target", "ja"),
                    new KeyValuePair<string, string>("format", "text"),
                });

                using (var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(GetErrorMessage(json, response.ReasonPhrase));
                    }

                    var payload = serializer.Deserialize<GoogleTranslateResponse>(json);
                    var translated = payload?.data?.translations?[0]?.translatedText;
                    if (string.IsNullOrWhiteSpace(translated))
                    {
                        throw new InvalidOperationException("Google 翻訳から結果を取得できませんでした。");
                    }

                    return HttpUtility.HtmlDecode(translated).Trim();
                }
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        string GetErrorMessage(string json, string fallback)
        {
            try
            {
                var payload = serializer.Deserialize<GoogleErrorEnvelope>(json);
                if (!string.IsNullOrWhiteSpace(payload?.error?.message))
                {
                    return payload.error.message;
                }
            }
            catch
            {
                // ignored
            }

            return string.IsNullOrWhiteSpace(fallback)
                ? "Google 翻訳に失敗しました。"
                : fallback;
        }

        sealed class GoogleTranslateResponse
        {
            public GoogleTranslateData data { get; set; }
        }

        sealed class GoogleTranslateData
        {
            public GoogleTranslateItem[] translations { get; set; }
        }

        sealed class GoogleTranslateItem
        {
            public string translatedText { get; set; }
        }

        sealed class GoogleErrorEnvelope
        {
            public GoogleError error { get; set; }
        }

        sealed class GoogleError
        {
            public string message { get; set; }
        }
    }
}
