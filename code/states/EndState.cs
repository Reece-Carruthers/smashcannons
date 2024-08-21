using Sandbox;

public class EndState : ExtendedState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Summary";
	[Sync] private TimeUntil RoundEndTime { get; set; }
	
	public string Winner { get; set; }

	protected override void OnEnter()
	{
		RoundEndTime = 5f;
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost ) return;

		FetchAlivePlayers();

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
