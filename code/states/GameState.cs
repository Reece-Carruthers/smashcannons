using Sandbox;

public class GameState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();
	[Sync] private TimeUntil RoundEndTime { get; set; }
	protected override void OnEnter()
	{
		Log.Info("in game state!"  );
		RoundEndTime = 90f;
	}
}
