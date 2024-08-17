using System;

public class LobbyState : BaseState
{
    public override int TimeLeft => RoundEndTime.Relative.CeilToInt();
    [Sync] public TimeUntil RoundEndTime { get; set; }
    
    public override string Name => "Waiting For Players . . .";
    private bool PlayedCountdown { get; set; }

    private HashSet<PlayerSpawn> usedCannonSpawnpoints = new HashSet<PlayerSpawn>();

    protected override void OnEnter()
    {
        if (!Networking.IsHost) return;
        
        RoundEndTime = 5f;
    }

    protected override void OnUpdate()
    {
        if (Networking.IsHost)
        {
            if (RoundEndTime)
            {
	            AssignPlayersToTeamsAndSpawnPoints();
                StateSystem.Set<GameState>();
                return;
            }
        }

        if (RoundEndTime <= 5f && !PlayedCountdown)
        {
            PlayedCountdown = true;
        }
        
        base.OnUpdate();
    }

    private void AssignPlayersToTeamsAndSpawnPoints()
    {
	    Log.Info( SmashCannon.CannonSpawnPoint );
	    Log.Info( SmashCannon.RunnerSpawnPoint );
	    var players = SmashCannon.Players.ToList();
	    var random = new Random();

	    if (players.Count < 1)
	    {
		    throw new Exception("Not enough players to assign to spawn points");
	    }

	    players = players.OrderBy(x => random.Next()).ToList();

	    var cannonPlayerCount = players.Count switch
	    {
		    1 => 1,
		    2 => 1,
		    > 3 => 2,
		    _ => 1
	    };

	    // Assign cannon players
	    for (var i = 0; i < cannonPlayerCount; i++)
	    {
		    var player = players[i];
		    player.TeamCategory = new SmashTeam();
		    player.Transform.LocalPosition = AssignCannonSpawnPoint(random);
	    }

	    // Assign runner players
	    for (var i = cannonPlayerCount; i < players.Count; i++)
	    {
		    Log.Info( "RUNNER PLAYER" );

		    var player = players[i];
		    player.TeamCategory = new RunnerTeam();
		    player.Transform.LocalPosition = AssignRunnerSpawnPoint(random);
	    }
    }


    private Vector3 AssignCannonSpawnPoint(Random random)
    {
        var cannonSpawns = SmashCannon.CannonSpawnPoint.ToList();

        if (usedCannonSpawnpoints.Count >= cannonSpawns.Count)
        {
            throw new Exception("All cannon spawnpoints are taken");
        }

        PlayerSpawn selectedSpawn = null;

        while (selectedSpawn == null)
        {
            var randomIndex = random.Next(cannonSpawns.Count);
            var potentialSpawn = cannonSpawns[randomIndex];

            if (!usedCannonSpawnpoints.Contains(potentialSpawn))
            {
                selectedSpawn = potentialSpawn;
                usedCannonSpawnpoints.Add(potentialSpawn);
            }
        }

        return selectedSpawn.Transform.Position;
    }

    private Vector3 AssignRunnerSpawnPoint(Random random)
    {
        var runnerSpawns =SmashCannon. RunnerSpawnPoint.ToList();

        if (runnerSpawns.Count == 0)
        {
            throw new Exception("No runner spawnpoints available");
        }

        var randomIndex = random.Next(runnerSpawns.Count);
        var selectedSpawn = runnerSpawns[randomIndex];

        return selectedSpawn.Transform.Position;
    }
}
