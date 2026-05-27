using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ManufacturingERP.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string EmbeddingUrl = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent";

    public EmbeddingService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _apiKey = LoadApiKey();
    }

    private static string LoadApiKey()
    {
        var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(jsonPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                var key = doc.RootElement.TryGetProperty("GeminiApiKey", out var prop) ? prop.GetString() : null;
                if (!string.IsNullOrWhiteSpace(key)) return key;
            }
            catch { }
        }
        return Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var requestBody = new
        {
            model = "models/text-embedding-004",
            content = new
            {
                parts = new[] { new { text } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{EmbeddingUrl}?key={_apiKey}";
        var response = await _httpClient.PostAsync(url, httpContent);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        var embeddingValues = doc.RootElement
            .GetProperty("embedding")
            .GetProperty("values")
            .EnumerateArray()
            .Select(v => (float)v.GetDouble())
            .ToArray();

        return embeddingValues;
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            var emb = await GenerateEmbeddingAsync(text);
            results.Add(emb);
        }
        return results;
    }
}
