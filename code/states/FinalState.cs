using Sandbox;

public class FinalState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Last Phase";
	[Sync] private TimeUntil RoundEndTime { get; set; }

	[Sync] public List<SmashRunnerMovement> ActiveRunnerPlayers { get; set; } = new List<SmashRunnerMovement>();
	[Sync] public List<SmashRunnerMovement> ActiveCannonPlayers { get; set; } = new List<SmashRunnerMovement>();

	protected override void OnEnter()
	{
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

	private void FetchAlivePlayerCount()
	{
		ActiveRunnerPlayers = SmashCannon.Players
			.Where( player => player.LifeState == LifeState.Alive && player.TeamCategory is RunnerTeam ).ToList();
		ActiveCannonPlayers = SmashCannon.Players
			.Where( player => player.LifeState == LifeState.Alive && player.TeamCategory is SmashTeam ).ToList();
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			FetchAlivePlayerCount();
			
			if (  Connection.All.Count > 1 && (ActiveRunnerPlayers.Count <= 0 || ActiveCannonPlayers.Count <= 0) )
			{
				StateSystem.Set<EndState>();
			}
		}
	}
}
