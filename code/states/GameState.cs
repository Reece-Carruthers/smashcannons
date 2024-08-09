using Sandbox;

public class GameState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();
	
	public override string Name => "Survive";
	[Sync] private TimeUntil RoundEndTime { get; set; }
	protected override void OnEnter()
	{
		Log.Info("in game state!"  );
		RoundEndTime = 5f;
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			if ( RoundEndTime )
			{
				StateSystem.Set<FinalState>();
				return;
			}
		}
	}

}
