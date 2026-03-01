using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TechWayFit.Pulse.AI.Options;

namespace TechWayFit.Pulse.AI.Http
{
    /// <summary>
    /// Centralises all HTTP communication with OpenAI-compatible endpoints.
    /// Supports both OpenAI direct (Bearer auth) and Azure OpenAI (api-key header + api-version query).
    /// </summary>
    public class OpenAIApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenAIOptions _options;
        private readonly ILogger<OpenAIApiClient> _logger;

        public OpenAIApiClient(
            IHttpClientFactory httpClientFactory,
            IOptions<OpenAIOptions> options,
            ILogger<OpenAIApiClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        // ── Credential resolution ─────────────────────────────────────────────

        /// <summary>
        /// Resolves the effective API key, base URL and model from config with optional per-request overrides
        /// (e.g. from the facilitator context).
        /// </summary>
        public (string? apiKey, string? baseUrl, string model) ResolveCredentials(
            string? keyOverride = null,
            string? urlOverride = null,
            string? modelOverride = null)
        {
            var apiKey  = keyOverride  ?? _options.ApiKey;
            var baseUrl = urlOverride  ?? _options.BaseUrl;
            var model   = modelOverride ?? _options.Model;
            return (apiKey, baseUrl, model);
        }

        // ── Standard POST ─────────────────────────────────────────────────────

        /// <summary>
        /// POST a payload to chat/completions and return the <see cref="HttpResponseMessage"/>.
        /// The caller is responsible for checking success and reading the body.
        /// </summary>
        public async Task<HttpResponseMessage> PostAsync(
            string apiKey,
            string? baseUrl,
            OpenAIChatRequest request,
            CancellationToken cancellationToken = default)
        {
            var client  = CreateConfiguredClient(apiKey, baseUrl);
            var content = Serialize(request);
            return await client.PostAsync(GetChatCompletionsPath(), content, cancellationToken);
        }

        /// <summary>
        /// POST a chat/completions request and return a fully-parsed <see cref="OpenAIChatResponse"/>.
        /// Throws <see cref="HttpRequestException"/> if the response is not successful.
        /// </summary>
        public async Task<OpenAIChatResponse> PostChatAsync(
            string apiKey,
            string? baseUrl,
            OpenAIChatRequest request,
            CancellationToken cancellationToken = default)
        {
            var resp = await PostAsync(apiKey, baseUrl, request, cancellationToken);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<OpenAIChatResponse>(body,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new OpenAIChatResponse();
        }

        // ── Streaming POST ────────────────────────────────────────────────────

        /// <summary>
        /// POST a streaming payload to chat/completions and yield each SSE delta content token.
        /// Returns an empty sequence on HTTP or network failure (caller decides fallback behaviour).
        /// </summary>
        public async IAsyncEnumerable<string> StreamAsync(
            string apiKey,
            string? baseUrl,
            OpenAIChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var client = CreateConfiguredClient(apiKey, baseUrl);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, GetChatCompletionsPath());
            httpRequest.Content = Serialize(request);

            HttpResponseMessage? resp = null;
            bool failed = false;
            try
            {
                resp = await client.SendAsync(
                    httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!resp.IsSuccessStatusCode)
                {
                    var errBody = await resp.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "OpenAI streaming call failed {Status}: {Error}", resp.StatusCode, errBody);
                    failed = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAIApiClient.StreamAsync: HTTP request failed");
                failed = true;
            }

            if (failed) yield break;

            using var stream = await resp!.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? line;
            while (!cancellationToken.IsCancellationRequested &&
                   (line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.StartsWith("data: "))  continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                string? token = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0)
                    {
                        var choice = choices[0];
                        if (choice.TryGetProperty("delta",   out var delta) &&
                            delta.TryGetProperty("content", out var contentEl))
                        {
                            token = contentEl.GetString();
                        }
                    }
                }
                catch { /* ignore malformed SSE chunk */ }

                if (!string.IsNullOrEmpty(token))
                    yield return token;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static StringContent Serialize(OpenAIChatRequest request) =>
            new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        private HttpClient CreateConfiguredClient(string apiKey, string? baseUrl)
        {
            var client = _httpClientFactory.CreateClient("openai");

            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                if (!baseUrl.EndsWith("/")) baseUrl += "/";
                client.BaseAddress = new Uri(baseUrl);
                _logger.LogDebug("OpenAIApiClient: using base URL {BaseUrl}", baseUrl);
            }

            // ApiKeyHeader controls which header carries the key.
            // Leave empty (default) for standard OpenAI Bearer auth.
            // Set to "api-key" for Azure OpenAI.
            var keyHeader = _options.ApiKeyHeader;
            if (!string.IsNullOrWhiteSpace(keyHeader))
            {
                client.DefaultRequestHeaders.Remove(keyHeader);
                client.DefaultRequestHeaders.Add(keyHeader, apiKey);
            }
            else
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }

            return client;
        }

        private string GetChatCompletionsPath()
        {
            // ApiQuery accepts any raw query string, e.g. "api-version=2024-02-01&foo=bar"
            return string.IsNullOrWhiteSpace(_options.ApiQuery)
                ? "chat/completions"
                : $"chat/completions?{_options.ApiQuery}";
        }
    }
}
