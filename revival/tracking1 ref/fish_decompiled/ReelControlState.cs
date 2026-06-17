internal sealed class ReelControlState
{
	public double Error { get; private set; }

	public double Control { get; private set; }

	public bool Holding { get; private set; }

	public ReelControlState(double error, double control, bool holding)
	{
		Error = error;
		Control = control;
		Holding = holding;
	}
}
