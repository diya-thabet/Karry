using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Karry.MathEngine.Client;

/// <summary>
/// Typed HTTP client for the Python FastAPI math engine. Handles serialization of
/// snake_case payloads expected by the engine and maps them back to C# records.
/// </summary>
public class KarryMathEngineClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _options;

    public KarryMathEngineClient(HttpClient httpClient, IOptions<MathEngineOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);

        _options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task<ConveyorResponse> ComputeConveyorAsync(
        ConveyorRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/engine/conveyor", request, _options, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ConveyorResponse>(_options, cancellationToken))!;
    }

    public async Task<RulResponse> ComputeRulAsync(
        RulRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/engine/rul", request, _options, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RulResponse>(_options, cancellationToken))!;
    }
}