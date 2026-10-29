using System;

// Population-based breathing room: killing an enemy does not immediately request a replacement.
public sealed class HordePressureCycle
{
    private readonly float reinforcementDuration;
    private readonly float minimumRestDuration;
    private readonly float resumeFraction;
    private readonly Func<int> chooseTarget;
    private float pressureUntil;
    private float restUntil;
    private int resumePopulation;

    public bool IsResting { get; private set; }
    public int TargetPopulation { get; private set; }

    public HordePressureCycle(float reinforcementDuration, float minimumRestDuration,
        float resumeFraction, Func<int> chooseTarget, float now)
    {
        this.reinforcementDuration = Math.Max(0.1f, reinforcementDuration);
        this.minimumRestDuration = Math.Max(0f, minimumRestDuration);
        this.resumeFraction = Math.Max(0f, Math.Min(0.95f, resumeFraction));
        this.chooseTarget = chooseTarget;
        BeginPressure(now);
    }

    public int AvailableSlots(float now, int alive)
    {
        alive = Math.Max(0, alive);
        if (IsResting)
        {
            if (now < restUntil || alive > resumePopulation) return 0;
            BeginPressure(now);
        }
        if (now >= pressureUntil || alive >= TargetPopulation)
        {
            IsResting = true;
            restUntil = now + minimumRestDuration;
            resumePopulation = (int)Math.Floor(Math.Min(alive, TargetPopulation) * resumeFraction);
            return 0;
        }
        return Math.Max(0, TargetPopulation - alive);
    }

    private void BeginPressure(float now)
    {
        IsResting = false;
        TargetPopulation = Math.Max(1, chooseTarget());
        pressureUntil = now + reinforcementDuration;
    }
}
