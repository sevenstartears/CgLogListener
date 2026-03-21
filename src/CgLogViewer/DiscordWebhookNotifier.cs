using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CgLogViewer
{
    public sealed class DiscordWebhookNotifier : IDisposable
    {
        readonly HttpClient httpClient = new HttpClient();
        readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public DiscordWebhookNotifier()
        {
            httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task SendAsync(string webhookUrl, string message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var payload = new Dictionary<string, string>
            {
                ["content"] = message,
                ["username"] = "CgLogViewer",
            };

            using (var content = new StringContent(serializer.Serialize(payload), Encoding.UTF8, "application/json"))
            using (var response = await httpClient.PostAsync(webhookUrl.Trim(), content, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
