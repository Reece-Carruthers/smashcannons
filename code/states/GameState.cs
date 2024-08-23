using Sandbox;

public class GameState : ExtendedState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Survive";
	[Sync] private TimeUntil RoundEndTime { get; set; }

	protected override void OnEnter()
	{
		RoundEndTime = 80f;
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			FetchAlivePlayers();
			
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
