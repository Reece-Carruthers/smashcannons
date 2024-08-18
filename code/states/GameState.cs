using Sandbox;

public class GameState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();
	
	public override string Name => "Survive";
	[Sync] private TimeUntil RoundEndTime { get; set; }
	protected override void OnEnter()
	{
		Log.Info("in game state!"  );
		RoundEndTime = 60f;
	}

	private void LogAliveRunners()
	{
		var aliveRunners = 0;
		
		// Iterate through all players in the scene
		foreach (var player in SmashCannon.Players)
		{
			// Check if the player is in the RunnerTeam and is alive
			if (player.TeamCategory is RunnerTeam && player.LifeState == LifeState.Alive)
			{
				aliveRunners++;
			}
		}

		// Log the number of alive runners
		Log.Info($"Alive Runners: {aliveRunners}");
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{

			LogAliveRunners();
			if ( RoundEndTime )
			{
				StateSystem.Set<FinalState>();
				return;
			}
		}
	}

}
