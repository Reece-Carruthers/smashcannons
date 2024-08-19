using System;

public class LobbyState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();
	[Sync] public TimeUntil RoundEndTime { get; set; }

	public override string Name => "Waiting For Players . . .";
	private bool PlayedCountdown { get; set; }

	private HashSet<PlayerSpawn> usedCannonSpawnpoints = new HashSet<PlayerSpawn>();

	[Sync] public List<SmashRunnerMovement> ActiveRunnerPlayers { get; set; } = new List<SmashRunnerMovement>();
	[Sync] public List<SmashRunnerMovement> ActiveCannonPlayers { get; set; } = new List<SmashRunnerMovement>();

	protected override void OnEnter()
	{
		RoundEndTime = 30f;
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			FetchAlivePlayerCount();
			var players = SmashCannon.Players.ToList();

			if ( RoundEndTime && players.Count <= 1 ) // Restart timer when there is not enough players
			{
				RoundEndTime = 30f;
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

		base.OnUpdate();
	}

	private void FetchAlivePlayerCount()
	{
		ActiveRunnerPlayers = SmashCannon.Players
			.Where( player => player.LifeState == LifeState.Alive && player.TeamCategory is RunnerTeam ).ToList();
		ActiveCannonPlayers = SmashCannon.Players
			.Where( player => player.LifeState == LifeState.Alive && player.TeamCategory is SmashTeam ).ToList();
	}

	private void AssignPlayersToTeamsAndSpawnPoints( List<SmashRunnerMovement> players )
	{
		var random = new Random();

		players = players.OrderBy( x => random.Next() ).ToList();

		var cannonPlayerCount = players.Count switch
		{
			1 => 1,
			2 => 1,
			> 3 => 2,
			_ => 1
		};

		// Assign cannon players
		for ( var i = 0; i < cannonPlayerCount; i++ )
		{
			var player = players[i];
			player.UpdateTeam( new SmashTeam() );
			player.CannonSpawnpoint = AssignCannonSpawnPoint( random );
			player.Respawn();
		}

		// Assign runner players
		for ( var i = cannonPlayerCount; i < players.Count; i++ )
		{
			var player = players[i];
			player.UpdateTeam( new RunnerTeam() );
			player.CannonSpawnpoint = new Vector3( 0 );
			player.Respawn();
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

		return selectedSpawn.Transform.Position;
	}
	
	 [Broadcast]
	 private void PlayCountdownSound()
	 {
	     Sound.Play("countdown");
	 }
}
