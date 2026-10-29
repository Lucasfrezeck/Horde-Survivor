using System;

// Run-local progression, independent of scene objects and UI.
public sealed class ExperienceProgress
{
    private static readonly int[] Costs = { 150, 190, 235, 295, 370, 460, 570, 715, 895 };
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int PendingChoices { get; private set; }
    public bool IsMaxLevel => Level >= 10;
    public int RequiredExperience => IsMaxLevel ? 0 : Costs[Level - 1];

    public void Add(int amount)
    {
        if (amount <= 0 || IsMaxLevel) return;
        long remaining = (long)Experience + amount;
        while (!IsMaxLevel && remaining >= RequiredExperience)
        {
            remaining -= RequiredExperience;
            Level++;
            PendingChoices++;
        }
        Experience = IsMaxLevel ? 0 : (int)remaining;
    }

    public bool ConsumeChoice()
    {
        if (PendingChoices <= 0) return false;
        PendingChoices--;
        return true;
    }
}
