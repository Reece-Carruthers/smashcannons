using Sandbox;

public class EndState : ExtendedState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Summary";
	[Sync] private TimeUntil RoundEndTime { get; set; }

	private bool updatedStats = false;

	private string statId;

	private List<SmashRunnerMovement> playersToUpdate;
	
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
				statId = "runners_wins";
				playersToUpdate = ActiveRunnerPlayers;
				PlayWinningTeam( "runners_win" );
			} else if ( ActiveCannonPlayers.Count >= 1 && ActiveRunnerPlayers.Count <= 0 )
			{
				statId = "cannon_wins";
				playersToUpdate = ActiveCannonPlayers;
				PlayWinningTeam( "cannon_win" );

			}
			else
			{
				statId = "draws1";
				playersToUpdate = ActiveRunnerPlayers.Concat( ActiveCannonPlayers ).ToList();
			}

			AddStat( playersToUpdate );
			
			updatedStats = true;
		}
		
		if ( RoundEndTime )
		{
			Game.ActiveScene.LoadFromFile( "scenes/smashtowermap.scene" );
		}
	}

	[Broadcast]
	private void AddStat( List<SmashRunnerMovement> players)
	{
		foreach ( var player in players )
		{
			player.AddStat( statId );
		}
	}

	[Broadcast(NetPermission.HostOnly)]
	private void PlayWinningTeam(string sound)
	{
		Sound.Play( sound );
	}
}
