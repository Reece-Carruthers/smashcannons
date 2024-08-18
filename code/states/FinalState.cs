using Sandbox;

public class FinalState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Last Phase";
	[Sync] private TimeUntil RoundEndTime { get; set; }
	protected override void OnEnter()
	{
		Log.Info("in final phase state!"  );
		RoundEndTime = 60f;

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
		}
	}
}
