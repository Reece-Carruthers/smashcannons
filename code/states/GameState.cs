using Sandbox;

public class GameState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Survive";
	[Sync] private TimeUntil RoundEndTime { get; set; }

	[Sync] public List<SmashRunnerMovement> ActiveRunnerPlayers { get; set; } = new List<SmashRunnerMovement>();
	[Sync] public List<SmashRunnerMovement> ActiveCannonPlayers { get; set; } = new List<SmashRunnerMovement>();

	protected override void OnEnter()
	{
		RoundEndTime = 5f;
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
			if ( RoundEndTime )
			{
				StateSystem.Set<FinalState>();
			}
			
			if (  Connection.All.Count > 1 && (ActiveRunnerPlayers.Count <= 0 || ActiveCannonPlayers.Count <= 0) )
			{
				StateSystem.Set<EndState>();
			}

			if ( Connection.All.Count <= 1 ) // Restart game on only having one connection
			{
				Game.ActiveScene.LoadFromFile( "scenes/smashtowermap.scene" );
			}
		}
	}
}
