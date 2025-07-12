/*
 * Copyright 2025 ClrSlate Tech labs Private Limited
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using ClrSlate.Mcp.KeyCloakServer.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ClrSlate.Mcp.KeyCloakServer.Services;

public class EmbeddingService
{
    private readonly SearchConfiguration _config;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public EmbeddingService(
        IOptions<SearchConfiguration> config,
        ILogger<EmbeddingService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ollama");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Empty text provided for embedding generation");
                return new float[_config.VectorDimensions];
            }

            _logger.LogDebug("Generating embedding for text: {TextPreview}...",
                text.Length > 50 ? text.Substring(0, 50) : text);

            var requestBody = new
            {
                model = _config.OllamaModel,
                prompt = text
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/embeddings", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseJson, _jsonOptions);

            if (embeddingResponse?.Embedding == null || embeddingResponse.Embedding.Length == 0)
            {
                _logger.LogError("No embedding returned from Ollama API");
                throw new InvalidOperationException("Empty embedding response from Ollama");
            }

            _logger.LogDebug("Successfully generated embedding with {Dimensions} dimensions", embeddingResponse.Embedding.Length);
            return embeddingResponse.Embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    public async Task<List<float[]>> GenerateBatchEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        _logger.LogInformation("Generating embeddings for {Count} texts", textList.Count);

        var embeddings = new List<float[]>();
        var batches = textList.Chunk(_config.BatchSize);

        foreach (var batch in batches)
        {
            var batchTasks = batch.Select(text => GenerateEmbeddingAsync(text, cancellationToken));
            var batchEmbeddings = await Task.WhenAll(batchTasks);
            embeddings.AddRange(batchEmbeddings);

            _logger.LogDebug("Completed batch of {BatchSize} embeddings", batch.Length);

            // Small delay to avoid overwhelming Ollama
            await Task.Delay(200, cancellationToken);
        }

        _logger.LogInformation("Successfully generated {Count} embeddings", embeddings.Count);
        return embeddings;
    }

    public async Task<bool> IsModelAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(responseJson, _jsonOptions);

            var modelExists = modelsResponse?.Models?.Any(m => m.Name.Contains(_config.OllamaModel)) ?? false;

            _logger.LogInformation("Model {Model} availability: {Available}", _config.OllamaModel, modelExists);
            return modelExists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if model {Model} is available", _config.OllamaModel);
            return false;
        }
    }

    public async Task EnsureModelAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (await IsModelAvailableAsync(cancellationToken))
        {
            _logger.LogInformation("Model {Model} is available", _config.OllamaModel);
            return;
        }

        _logger.LogWarning("Model {Model} not found. Please run: ollama pull {Model}", _config.OllamaModel, _config.OllamaModel);
        throw new InvalidOperationException($"Model {_config.OllamaModel} is not available. Run: ollama pull {_config.OllamaModel}");
    }
}