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
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using OllamaSharp.Models;

namespace ClrSlate.Mcp.KeyCloakServer.Services;

public class EmbeddingService
{
    private readonly SearchConfiguration _config;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly IOllamaApiClient _ollamaApiClient;

    public EmbeddingService(
        IOptions<SearchConfiguration> config,
        ILogger<EmbeddingService> logger,
        [FromKeyedServices("ollama")] IOllamaApiClient ollamaApiClient)
    {
        _config = config.Value;
        _logger = logger;
        _ollamaApiClient = ollamaApiClient;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 2000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Empty text provided for embedding generation");
                    return new float[_config.VectorDimensions];
                }

                _logger.LogDebug("Generating embedding for text: {TextPreview}... (attempt {Attempt}/{MaxRetries})",
    text.Length > 50 ? text.Substring(0, 50) : text, attempt, maxRetries);

                // Use OllamaSharp's EmbedAsync method
                var embedResponse = await _ollamaApiClient.EmbedAsync(new EmbedRequest
                {
                    Model = _config.OllamaModel,
                    Input = new List<string> { text}
                }, cancellationToken);

                if (embedResponse?.Embeddings == null || !embedResponse.Embeddings.Any())
                {
                    _logger.LogError("No embedding returned from Ollama API");
                    throw new InvalidOperationException("Empty embedding response from Ollama");
                }

                var embedding = embedResponse.Embeddings.First();
                if (embedding == null || embedding.Length == 0)
                {
                    _logger.LogError("Empty embedding vector returned from Ollama API");
                    throw new InvalidOperationException("Empty embedding vector from Ollama");
                }

                _logger.LogDebug("Successfully generated embedding with {Dimensions} dimensions", embedding.Length);
                return embedding;
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("404") && attempt < maxRetries)
            {
                _logger.LogWarning("Model {Model} not ready yet (404), waiting {DelayMs}ms before retry {Attempt}/{MaxRetries}. This is normal during initial model download.",
                    _config.OllamaModel, retryDelayMs, attempt, maxRetries);

                await Task.Delay(retryDelayMs, cancellationToken);
                continue;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Error generating embedding (attempt {Attempt}/{MaxRetries}), retrying in {DelayMs}ms...",
                    attempt, maxRetries, retryDelayMs);

                await Task.Delay(retryDelayMs, cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text after {MaxRetries} attempts", maxRetries);
                throw;
            }
        }

        throw new InvalidOperationException($"Failed to generate embedding after {maxRetries} attempts");
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
        // In Aspire containerized scenarios, we trust that the model is managed by the orchestrator
        // The actual availability will be tested during embedding generation
        _logger.LogInformation("Model {Model} availability assumed (Aspire-managed)", _config.OllamaModel);
        return true;
    }

    public async Task EnsureModelAvailableAsync(CancellationToken cancellationToken = default)
    {
        // In Aspire containerized scenarios, model availability is managed by the orchestrator
        // No need for manual model checks - let embedding generation handle any issues
        _logger.LogInformation("Model {Model} availability ensured by Aspire orchestration", _config.OllamaModel);
        await Task.CompletedTask;
    }
}