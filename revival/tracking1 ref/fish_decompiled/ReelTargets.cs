internal sealed class ReelTargets
{
	public ulong Reel { get; private set; }

	public ulong Fish { get; private set; }

	public ulong Playerbar { get; private set; }

	public ulong Container { get; private set; }

	public ReelTargets(ulong reel, ulong fish, ulong playerbar, ulong container)
	{
		Reel = reel;
		Fish = fish;
		Playerbar = playerbar;
		Container = container;
	}
}
