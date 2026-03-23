using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CgLogViewer
{
    public static class DeepLGlossaryManager
    {
        const string ManagedGlossaryName = "CgLogViewer";
        static readonly Uri GlossariesUri = new Uri("https://api-free.deepl.com/v2/glossaries");
        static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        static readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public static async Task<string> UploadAsync(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                throw new InvalidOperationException("DeepL API キーが設定されていません。");
            }

            var entries = TranslationDictionaryStore.Instance.LoadEntries();
            string glossaryEntries = string.Join("\n", entries.Select(entry => $"{entry.SourceTerm}\t{entry.TargetTerm}"));
            if (string.IsNullOrWhiteSpace(glossaryEntries))
            {
                throw new InvalidOperationException("アップロードする辞書項目がありません。");
            }

            await DeleteManagedGlossariesAsync(authKey.Trim()).ConfigureAwait(false);

            using (var request = new HttpRequestMessage(HttpMethod.Post, GlossariesUri))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {authKey.Trim()}");
                request.Content = new StringContent(serializer.Serialize(new
                {
                    name = ManagedGlossaryName,
                    source_lang = "ZH",
                    target_lang = "JA",
                    entries = glossaryEntries,
                    entries_format = "tsv"
                }), Encoding.UTF8, "application/json");

                using (var response = await httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(string.IsNullOrWhiteSpace(json) ? response.ReasonPhrase : json);
                    }

                    var payload = serializer.Deserialize<CreateGlossaryResponse>(json);
                    if (payload == null || string.IsNullOrWhiteSpace(payload.glossary_id))
                    {
                        throw new InvalidOperationException("DeepL 用語集の作成結果を取得できませんでした。");
                    }

                    return payload.glossary_id;
                }
            }
        }

        static async Task DeleteManagedGlossariesAsync(string authKey)
        {
            var settings = Settings.GetInstance();
            var glossaryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(settings.DeepLGlossaryId))
            {
                glossaryIds.Add(settings.DeepLGlossaryId);
            }

            foreach (var glossary in await ListGlossariesAsync(authKey).ConfigureAwait(false))
            {
                if (string.Equals(glossary.name, ManagedGlossaryName, StringComparison.OrdinalIgnoreCase) ||
                    glossary.name.StartsWith(ManagedGlossaryName + " ", StringComparison.OrdinalIgnoreCase))
                {
                    glossaryIds.Add(glossary.glossary_id);
                }
            }

            foreach (var glossaryId in glossaryIds)
            {
                await DeleteGlossaryAsync(authKey, glossaryId).ConfigureAwait(false);
            }
        }

        static async Task<IReadOnlyList<GlossaryInfo>> ListGlossariesAsync(string authKey)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, GlossariesUri))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {authKey}");

                using (var response = await httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(string.IsNullOrWhiteSpace(json) ? response.ReasonPhrase : json);
                    }

                    var payload = serializer.Deserialize<ListGlossariesResponse>(json);
                    return payload?.glossaries ?? Array.Empty<GlossaryInfo>();
                }
            }
        }

        static async Task DeleteGlossaryAsync(string authKey, string glossaryId)
        {
            if (string.IsNullOrWhiteSpace(glossaryId))
            {
                return;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Delete, new Uri($"{GlossariesUri}/{glossaryId}")))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {authKey}");

                using (var response = await httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return;
                    }

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(json) ? response.ReasonPhrase : json);
                }
            }
        }

        sealed class CreateGlossaryResponse
        {
            public string glossary_id { get; set; }
        }

        sealed class ListGlossariesResponse
        {
            public GlossaryInfo[] glossaries { get; set; }
        }

        sealed class GlossaryInfo
        {
            public string glossary_id { get; set; }
            public string name { get; set; }
        }
    }
}
