using System;
using System.Collections.Generic;
using System.Collections;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CgLogViewer
{
    public sealed class OpenAITranslationService : ITranslationService, IDisposable
    {
        static readonly Uri ResponsesUri = new Uri("https://api.openai.com/v1/responses");
        const string Model = "gpt-5-mini";
        const string TranslationInstructions = "You are a translation engine for a game log viewer. Translate the user's text into natural Japanese. Return only the translated text. Preserve timestamps, URLs, player names, bracket tags, line breaks, punctuation, and game-specific proper nouns whenever possible. Do not add explanations or quotes.";

        readonly HttpClient httpClient = new HttpClient();
        readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public OpenAITranslationService()
        {
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public TranslationProvider Provider => TranslationProvider.OpenAI;

        public async Task<string> TranslateToJapaneseAsync(string authKey, string text, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                throw new InvalidOperationException("OpenAI API キーが設定されていません。");
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = Model,
                ["instructions"] = TranslationInstructions,
                ["input"] = text,
            };

            using (var request = new HttpRequestMessage(HttpMethod.Post, ResponsesUri))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authKey.Trim());
                request.Content = new StringContent(serializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                using (var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(GetErrorMessage(json, response.ReasonPhrase));
                    }

                    string translated = ExtractTranslatedText(json);
                    if (string.IsNullOrWhiteSpace(translated))
                    {
                        throw new InvalidOperationException("OpenAI から翻訳結果を取得できませんでした。");
                    }

                    return translated.Trim();
                }
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        string ExtractTranslatedText(string json)
        {
            var payload = serializer.DeserializeObject(json) as Dictionary<string, object>;
            if (payload == null)
            {
                return null;
            }

            object outputText;
            if (payload.TryGetValue("output_text", out outputText) && outputText is string directText && !string.IsNullOrWhiteSpace(directText))
            {
                return directText;
            }

            object output;
            if (!payload.TryGetValue("output", out output))
            {
                return null;
            }

            var fragments = new List<string>();
            AppendOutputText(output, fragments);
            return string.Join(Environment.NewLine, fragments.FindAll(fragment => !string.IsNullOrWhiteSpace(fragment))).Trim();
        }

        void AppendOutputText(object node, List<string> fragments)
        {
            if (node == null)
            {
                return;
            }

            if (node is string)
            {
                return;
            }

            var dictionary = node as Dictionary<string, object>;
            if (dictionary != null)
            {
                object textValue;
                if (dictionary.TryGetValue("text", out textValue))
                {
                    if (textValue is string text && !string.IsNullOrWhiteSpace(text))
                    {
                        fragments.Add(text);
                    }
                    else
                    {
                        var textObject = textValue as Dictionary<string, object>;
                        if (textObject != null &&
                            textObject.TryGetValue("value", out var valueObject) &&
                            valueObject is string valueText &&
                            !string.IsNullOrWhiteSpace(valueText))
                        {
                            fragments.Add(valueText);
                        }
                    }
                }

                object contentValue;
                if (dictionary.TryGetValue("content", out contentValue))
                {
                    AppendOutputText(contentValue, fragments);
                }

                return;
            }

            var sequence = node as IEnumerable;
            if (sequence != null)
            {
                foreach (var item in sequence)
                {
                    AppendOutputText(item, fragments);
                }

                return;
            }
        }

        string GetErrorMessage(string json, string fallback)
        {
            try
            {
                var payload = serializer.DeserializeObject(json) as Dictionary<string, object>;
                if (payload != null &&
                    payload.TryGetValue("error", out var errorObject) &&
                    errorObject is Dictionary<string, object> error &&
                    error.TryGetValue("message", out var messageObject) &&
                    messageObject is string message &&
                    !string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
            catch
            {
                // ignored
            }

            return string.IsNullOrWhiteSpace(fallback)
                ? "OpenAI での翻訳に失敗しました。"
                : fallback;
        }
    }
}
