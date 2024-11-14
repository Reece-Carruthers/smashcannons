using Sandbox;

public class EndState : ExtendedState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Summary";
	[Sync] private TimeUntil RoundEndTime { get; set; }

	private bool updatedStats = false;
	
	protected override void OnEnter()
	{
		RoundEndTime = 5f;
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost ) return;

		FetchAlivePlayers();

		if ( !updatedStats )
		{
			if ( ActiveRunnerPlayers.Count >= 1 && ActiveCannonPlayers.Count <= 0 )
			{
				PlayWinningTeam( "runners_win" );
			} else if ( ActiveCannonPlayers.Count >= 1 && ActiveRunnerPlayers.Count <= 0 )
			{
				PlayWinningTeam( "cannon_win" );
			}
			
			updatedStats = true;
		}
		
		if ( RoundEndTime )
		{
			Game.ActiveScene.LoadFromFile( "scenes/smashtowermap.scene" );
		}
	}

	[Broadcast(NetPermission.HostOnly)]
	private void PlayWinningTeam(string sound)
	{
		Sound.Play( sound );
	}
}
