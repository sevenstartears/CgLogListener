using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CgLogListener
{
    public sealed class DeepLTranslationService : ITranslationService, IDisposable
    {
        static readonly Uri TranslateUri = new Uri("https://api-free.deepl.com/v2/translate");
        readonly HttpClient httpClient = new HttpClient();
        readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public DeepLTranslationService()
        {
            httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public TranslationProvider Provider => TranslationProvider.DeepL;

        public async Task<string> TranslateToJapaneseAsync(string authKey, string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                throw new InvalidOperationException("DeepL API キーが設定されていません。");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, TranslateUri))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {authKey.Trim()}");
                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("text", text),
                    new KeyValuePair<string, string>("target_lang", "JA"),
                    new KeyValuePair<string, string>("preserve_formatting", "1"),
                    new KeyValuePair<string, string>("split_sentences", "0"),
                });

                using (var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(GetErrorMessage(json, response.ReasonPhrase));
                    }

                    var payload = serializer.Deserialize<DeepLTranslateResponse>(json);
                    var translated = payload?.translations?.FirstOrDefault()?.text;
                    if (string.IsNullOrWhiteSpace(translated))
                    {
                        throw new InvalidOperationException("DeepL から翻訳結果を取得できませんでした。");
                    }

                    return translated.Trim();
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
                var payload = serializer.Deserialize<DeepLErrorResponse>(json);
                if (!string.IsNullOrWhiteSpace(payload?.message))
                {
                    return payload.message;
                }
            }
            catch
            {
                // ignored
            }

            return string.IsNullOrWhiteSpace(fallback)
                ? "DeepL 翻訳に失敗しました。"
                : fallback;
        }

        sealed class DeepLTranslateResponse
        {
            public List<DeepLTranslationItem> translations { get; set; }
        }

        sealed class DeepLTranslationItem
        {
            public string text { get; set; }
        }

        sealed class DeepLErrorResponse
        {
            public string message { get; set; }
        }
    }
}
