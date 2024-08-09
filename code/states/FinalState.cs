using Sandbox;

public class FinalState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Last Phase";
	[Sync] private TimeUntil RoundEndTime { get; set; }
	protected override void OnEnter()
	{
		Log.Info("in final phase state!"  );
		RoundEndTime = 10f;

		var mainScript = SmashCannon.Instance;
		if ( mainScript != null && mainScript.Platforms != null )
		{
			mainScript.Platforms.Enabled = false;
		}
		if ( mainScript != null && mainScript.Pillars != null )
		{
			mainScript.Pillars.Enabled = false;
		}
		if ( mainScript != null && mainScript.Ramp != null )
		{
			mainScript.Ramp.Enabled = true;
		}
	}
}
