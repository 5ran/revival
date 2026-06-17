using System.Reflection;

namespace Client.Services.Fishing;

// Requiem rod: auto-applies a 175ms minimum interval between hold/release
// flips.
[Obfuscation(Exclude = true, ApplyToMembers = true)]
internal sealed class RequiemRodProfile : RodProfile
{
    public override RodKind Kind => RodKind.Requiem;

    public override int FishingActionDelayMs => 175;
}
