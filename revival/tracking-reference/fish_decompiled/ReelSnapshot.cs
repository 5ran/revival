using System.Drawing;

internal sealed class ReelSnapshot
{
	public RectangleF Container { get; private set; }

	public float FishCenterX { get; private set; }

	public float PlayerbarCenterX { get; private set; }

	public float PlayerbarWidth { get; private set; }

	public ReelSnapshot(RectangleF container, float fishCenterX, float playerbarCenterX, float playerbarWidth)
	{
		Container = container;
		FishCenterX = fishCenterX;
		PlayerbarCenterX = playerbarCenterX;
		PlayerbarWidth = playerbarWidth;
	}
}
