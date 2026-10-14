
using System;
using System.Threading;
using RRC.Abstractions;

namespace RRC.Student;

public sealed class AdaptiveRequestPolicy : IRequestPolicy
{
    // Состояние 1: количество последовательных тайм-аутов.
    private int _consecutiveTimeouts;

    // Состояние 2: текущий адаптивный тайм-аут.
    private int _adaptiveTimeoutMs = 160;

    public TimeSpan GetTimeout(RequestContext context, Random random)
    {
        // Тайм-аут определяется накопленным состоянием,
        // а не номером попытки.
        return TimeSpan.FromMilliseconds(
            Volatile.Read(ref _adaptiveTimeoutMs));
    }

    public RetryDecision OnResult(
        RequestObservation observation,
        Random random)
    {
        if (observation.Outcome == RequestOutcome.Timeout)
        {
            int timeouts = Interlocked.Increment(
                ref _consecutiveTimeouts);

            // При повторяющихся тайм-аутах осторожно увеличиваем ожидание.
            if (timeouts >= 2)
            {
                int current = Volatile.Read(ref _adaptiveTimeoutMs);
                int updated = Math.Min(240, current + 20);

                Interlocked.CompareExchange(
                    ref _adaptiveTimeoutMs, updated, current);
            }

            // Не создаём дополнительные повторы после повторной попытки.
            if (observation.Attempt == 1)
            {
                return RetryDecision.RetryAfter(
                    TimeSpan.FromMilliseconds(100));
            }

            return RetryDecision.NoRetry();
        }

        // Успешные ответы сбрасывают серию тайм-аутов.
        Interlocked.Exchange(ref _consecutiveTimeouts, 0);

        // Постепенно возвращаем тайм-аут к базовому значению.
        int previous = Volatile.Read(ref _adaptiveTimeoutMs);
        int reduced = Math.Max(160, previous - 10);

        Interlocked.CompareExchange(
            ref _adaptiveTimeoutMs, reduced, previous);

        return RetryDecision.NoRetry();
    }
}
