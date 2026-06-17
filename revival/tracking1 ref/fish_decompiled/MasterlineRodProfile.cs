namespace Client.Services.Fishing;

// Masterline currently follows default fishing behavior.
internal sealed class MasterlineRodProfile : RodProfile
{
    public override RodKind Kind => RodKind.MasterlineRod;

    public override int FishingActionDelayMs => 90;
}

