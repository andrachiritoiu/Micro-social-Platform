using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace MicroSocialPlatform.Services
{
    public class GeminiCommentValidationService : ICommentValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // Endpoint-ul oficial pentru Gemini
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

        private const string Model = "gemini-2.5-flash-lite";

        public GeminiCommentValidationService(IConfiguration config)
        {
            // Luăm cheia din appsettings.json
            _apiKey = config["GoogleAI:ApiKey"]
                ?? throw new Exception("Lipsește cheia GoogleAI:ApiKey din appsettings.json");

            // Cream HttpClient 
            _httpClient = new HttpClient();
        }

        public async Task<bool> IsCommentValidAsync(string text)
        {
            // Dacă textul e gol, îl considerăm invalid 
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // PROMPT pentru moderare:
            var prompt =
                $@"You are a strict content moderation system for a social media application.

                The comment is NOT acceptable (answer NO) if it contains:
                - insults or derogatory language (e.g. ""prost"", ""idiot"", ""penibil"", ""jalnic"", ""de rahat"")
                - harassment or offensive expressions
                - hate speech, threats, or sexual explicit content
                - aggressive or humiliating wording, even if it refers to the application or content

                The comment IS acceptable (answer YES) ONLY if it is respectful and polite,
                even when expressing criticism.

                Answer with ONLY: YES or NO.

                Comment:
                {text}";


            // Construim body-ul JSON în formatul cerut de Gemini:
            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
          
                generationConfig = new
                {
                    temperature = 0.0,
                    maxOutputTokens = 10
                }
            };

            // Serializăm body-ul la JSON
            var json = JsonSerializer.Serialize(body);

            // Îl trimitem ca application/json
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // URL final cu cheia în query param 
            var url = $"{BaseUrl}{Model}:generateContent?key={_apiKey}";

            // Request POST către Gemini
            var response = await _httpClient.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();

            // Dacă API-ul a dat eroare, aruncăm excepție 
            if (!response.IsSuccessStatusCode)
            {
                // dacă AI nu răspunde, NU blocăm aplicația
                return true;
            }

            // Parsăm răspunsul:
            // candidates[0].content.parts[0].text
            using var doc = JsonDocument.Parse(responseText);

            var answer = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            // Normalizăm răspunsul
            answer = (answer ?? "").Trim().ToUpperInvariant();

            // Dacă răspunsul e YES => comentariul e valid
            return answer.StartsWith("YES");
        }
    }
}
