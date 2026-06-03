namespace RezSaaS.Api.PublicApi;

public sealed class PublicSlotSearchOptions
{
    public const string SectionName = "PublicSlotSearch";

    public int MaxSlots { get; init; } = 64;

    public int SlotIntervalMinutes { get; init; } = 15;
}
