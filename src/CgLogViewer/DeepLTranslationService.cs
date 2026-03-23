using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CgLogViewer
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

        public async Task<string> TranslateToJapaneseAsync(string authKey, string text, CancellationToken cancellationToken, OpenAIReasoningEffort reasoningEffort)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                throw new InvalidOperationException("DeepL API キーが設定されていません。");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var settings = Settings.GetInstance();
            bool useGlossary = !string.IsNullOrWhiteSpace(settings.DeepLGlossaryId);
            var maskResult = useGlossary
                ? new TranslationDictionaryMaskResult { MaskedText = text }
                : TranslationDictionaryMasker.Mask(text, TranslationDictionaryStore.Instance.LoadEntries());
            string requestText = maskResult.MaskedText;

            using (var request = new HttpRequestMessage(HttpMethod.Post, TranslateUri))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {authKey.Trim()}");
                var formValues = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("text", requestText),
                    new KeyValuePair<string, string>("target_lang", "JA"),
                    new KeyValuePair<string, string>("preserve_formatting", "1"),
                    new KeyValuePair<string, string>("split_sentences", "0"),
                };
                if (useGlossary)
                {
                    formValues.Add(new KeyValuePair<string, string>("source_lang", "ZH"));
                    formValues.Add(new KeyValuePair<string, string>("glossary_id", settings.DeepLGlossaryId));
                }
                request.Content = new FormUrlEncodedContent(formValues);

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

                    return TranslationDictionaryMasker.Restore(translated.Trim(), maskResult.PlaceholderMap);
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
