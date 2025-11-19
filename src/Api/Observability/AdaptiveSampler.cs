using OpenTelemetry.Trace;

namespace Netemplate.Api.Observability;

public sealed class AdaptiveSampler : Sampler
{
    private readonly double _baseSamplingProbability;

    public AdaptiveSampler(double baseSamplingProbability = 0.1) // 10% default
    {
        _baseSamplingProbability = baseSamplingProbability;
    }

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        var activityName = samplingParameters.Name;

        // Always sample errors and health checks
        if (activityName.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            activityName.Contains("/health/", StringComparison.OrdinalIgnoreCase))
        {
            return new SamplingResult(SamplingDecision.RecordAndSample);
        }

        // Apply probabilistic sampling for normal requests
        var random = Random.Shared.NextDouble();
        return random < _baseSamplingProbability
            ? new SamplingResult(SamplingDecision.RecordAndSample)
            : new SamplingResult(SamplingDecision.Drop);
    }
}
