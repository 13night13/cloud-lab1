
using RRC.Abstractions;

namespace RRC.Student;

public sealed class AdaptiveRequestPolicy : IRequestPolicy
{
    public TimeSpan GetTimeout(RequestContext context, Random random)
    {
        // Первая попытка: быстрый стандартный тайм-аут.
        if (context.Attempt <= 1)
        {
            return TimeSpan.FromMilliseconds(160);
        }

        // Повторная попытка: даём серверу больше времени.
        return TimeSpan.FromMilliseconds(240);
    }

    public RetryDecision OnResult(
        RequestObservation observation,
        Random random)
    {
        // Повторяем только после тайм-аута первой попытки.
        if (observation.Outcome == RequestOutcome.Timeout
            && observation.Attempt == 1)
        {
            return RetryDecision.RetryAfter(
                TimeSpan.FromMilliseconds(100));
        }

        return RetryDecision.NoRetry();
    }
}
