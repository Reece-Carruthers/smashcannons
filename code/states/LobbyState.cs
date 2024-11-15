using System;

public class LobbyState : ExtendedState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();
	[Sync] public TimeUntil RoundEndTime { get; set; }

	public override string Name => "Waiting for players...";
	private bool PlayedCountdown { get; set; }

	private HashSet<PlayerSpawn> usedCannonSpawnpoints = new HashSet<PlayerSpawn>();

	protected override void OnEnter()
	{
		RoundEndTime = 15f;
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			FetchAlivePlayers();
			var players = SmashCannon.Players.ToList();

			if ( RoundEndTime && players.Count <= 1 ) // Restart timer when there is not enough players
			{
				RoundEndTime = 15f;
			}

			if ( RoundEndTime && players.Count > 1 )
			{
				AssignPlayersToTeamsAndSpawnPoints( players );
				StateSystem.Set<GameState>();
				return;
			}
			
			if ( RoundEndTime <= 3f && !PlayedCountdown && players.Count > 1)
			{
				PlayedCountdown = true;
				PlayCountdownSound();
			}
		}
	}
	

	private void AssignPlayersToTeamsAndSpawnPoints( List<SmashRunnerMovement> players )
	{
		var random = new Random();

		players = players.OrderBy( x => random.Next() ).ToList();

		var cannonPlayerCount = players.Count switch
		{
			1 => 1,
			2 => 1,
			> 4 => 2,
			_ => 2
		};

		// Assign cannon players
		for ( var i = 0; i < cannonPlayerCount; i++ )
		{
			var player = players[i];
			player.UpdateTeam(new SmashTeam());
			player.Respawn(AssignCannonSpawnPoint( random ));
		}

		// Assign runner players
		for ( var i = cannonPlayerCount; i < players.Count; i++ )
		{
			var player = players[i];
			player.UpdateTeam(new RunnerTeam());
			player.Respawn(AssignRunnerSpawnPoint( random ));
		}
	}

	private Vector3 AssignCannonSpawnPoint( Random random )
	{
		var cannonSpawns = SmashCannon.CannonSpawnPoint.ToList();

		PlayerSpawn selectedSpawn = null;

		while ( selectedSpawn == null )
		{
			var randomIndex = random.Next( cannonSpawns.Count );
			var potentialSpawn = cannonSpawns[randomIndex];

			if ( !usedCannonSpawnpoints.Contains( potentialSpawn ) )
			{
				selectedSpawn = potentialSpawn;
				usedCannonSpawnpoints.Add( potentialSpawn );
			}
		}

		return selectedSpawn.WorldPosition;
	}
	
	private Vector3 AssignRunnerSpawnPoint(Random random)
	{
		var runnerSpawns = Game.ActiveScene.Components.GetAll<PlayerSpawn>().Where( x => x.Tags.Has( "runner_spawn" ) )
			.ToList();

		var randomIndex = random.Next( runnerSpawns.Count );
		var selectedSpawn = runnerSpawns[randomIndex];

		return selectedSpawn.WorldPosition;
	}
	
	 [Broadcast]
	 private void PlayCountdownSound()
	 {
	     Sound.Play("countdown");
	 }
}
