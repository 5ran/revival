namespace Client.Services.Fishing;

// The standard rod - no target adjustment, no inversion, with a light action
// delay to prevent rare release chatter from dropping the bar.
internal sealed class DefaultRodProfile : RodProfile
{
    public override RodKind Kind => RodKind.Default;

    public override int FishingActionDelayMs => 90;
}
