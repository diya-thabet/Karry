using System.Text.Json;
using System.Text.Json.Serialization;

namespace Karry.MathEngine.Client;

public sealed class MathEngineOptions
{
    public const string SectionName = "MathEngine";

    public string BaseUrl { get; set; } = "http://localhost:8000";

    public int TimeoutSeconds { get; set; } = 30;
}

public sealed record ConveyorRequest(
    [property: JsonPropertyName("q_nominal")] double QNominal,
    [property: JsonPropertyName("phi_wear")] double? PhiWear = null,
    [property: JsonPropertyName("psi_inclination")] double? PsiInclination = null,
    [property: JsonPropertyName("omega_weather")] double? OmegaWeather = null);

public sealed record ConveyorResponse(
    [property: JsonPropertyName("q_belt")] double QBelt);

public sealed record RulRequest(
    [property: JsonPropertyName("rating_usage")] double RatingUsage,
    [property: JsonPropertyName("accumulated_usage")] double AccumulatedUsage,
    [property: JsonPropertyName("daily_usage")] double DailyUsage,
    [property: JsonPropertyName("rating_mass")] double RatingMass,
    [property: JsonPropertyName("processed_mass")] double ProcessedMass,
    [property: JsonPropertyName("daily_mass")] double DailyMass,
    [property: JsonPropertyName("bond_abrasion_index")] double BondAbrasionIndex = 1.0);

public sealed record RulResponse(
    [property: JsonPropertyName("rul_days")] double RulDays);