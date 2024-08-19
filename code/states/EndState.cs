using Sandbox;

public class EndState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "End Game";
	[Sync] private TimeUntil RoundEndTime { get; set; }

	[Sync] public List<SmashRunnerMovement> ActiveRunnerPlayers { get; set; } = new List<SmashRunnerMovement>();
	[Sync] public List<SmashRunnerMovement> ActiveCannonPlayers { get; set; } = new List<SmashRunnerMovement>();
	
	public string Winner { get; set; }

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

			if ( ActiveRunnerPlayers.Count >= 1 && ActiveCannonPlayers.Count <= 0 )
			{
				Winner = "Runners win!";
			} else if ( ActiveCannonPlayers.Count >= 1 && ActiveRunnerPlayers.Count <= 0 )
			{
				Winner = "Cannoniers win!";
			}
			else
			{
				Winner = "No one wins!";
			}

			if ( RoundEndTime )
			{
				Game.ActiveScene.LoadFromFile( "scenes/smashtowermap.scene" );
			}
		}
	}
}
