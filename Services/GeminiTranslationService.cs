using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace MicroSocialPlatform.Services
{
    public class GeminiTranslationService : ITranslationService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly IMemoryCache _cache;

        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string DefaultModel = "gemini-2.5-flash-lite";

        public GeminiTranslationService(HttpClient http, IConfiguration config, IMemoryCache cache)
        {
            _http = http;
            _cache = cache;

            _apiKey = config["GoogleAI:ApiKey"]
                ?? throw new Exception("Lipsește cheia GoogleAI:ApiKey din appsettings.json");
        }

        private static string LangCodeToName(string lang) => lang switch
        {
            "ro" => "Romanian",
            "en" => "English",
            "fr" => "French",
            "de" => "German",
            "es" => "Spanish",
            "it" => "Italian",
            _ => lang
        };

        public async Task<string> TranslateAsync(string text, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;


            var cacheKey = BuildCacheKey(text, targetLang);

            if (_cache.TryGetValue(cacheKey, out string cached))
                return cached;

            var targetLanguageName = LangCodeToName(targetLang);

            var prompt =
                $@"You are a professional translation engine.

                Translate the message into {targetLanguageName}.
                Return ONLY the translated text.
                Do NOT add explanations, quotes, or extra text.
                Preserve punctuation, emojis, URLs, @mentions and #hashtags.

                Message:
                {text}";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.0,
                    maxOutputTokens = 256
                }
            };

            var json = JsonSerializer.Serialize(body);

            try
            {
                var response = await _http.PostAsync(
                    $"{BaseUrl}{DefaultModel}:generateContent?key={_apiKey}",
                    new StringContent(json, Encoding.UTF8, "application/json")
                );

                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // putem pune un cache scurt ca să nu spamăm API-ul când e 429
                    _cache.Set(cacheKey, text, TimeSpan.FromSeconds(20));
                    return text;
                }

                using var doc = JsonDocument.Parse(responseText);

                var translated = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                var result = string.IsNullOrWhiteSpace(translated) ? text : translated.Trim();
                // cache lung
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

                return result;
            }
            catch
            {
                // orice eroare de network/parsing -> fallback + cache scurt
                _cache.Set(cacheKey, text, TimeSpan.FromSeconds(20));
                return text;
            }
        }

        private static string BuildCacheKey(string text, string targetLang)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            var hash = Convert.ToHexString(bytes);
            return $"tr:{targetLang}:{hash}";
        }
    }
}
