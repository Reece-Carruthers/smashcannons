using System;

public class LobbyState : BaseState
{
    public override int TimeLeft => RoundEndTime.Relative.CeilToInt();
    [Sync] public TimeUntil RoundEndTime { get; set; }
    
    private bool PlayedCountdown { get; set; }

    public IEnumerable<PlayerSpawn> RunnerSpawnPoint =>
        Scene.Components.GetAll<PlayerSpawn>().Where(x => x.Tags.Has("runner_spawn"));

    public IEnumerable<PlayerSpawn> CannonSpawnPoint =>
        Scene.Components.GetAll<PlayerSpawn>().Where(x => x.Tags.Has("cannon_spawn"));

    private HashSet<PlayerSpawn> usedCannonSpawnpoints = new HashSet<PlayerSpawn>();

    protected override void OnEnter()
    {
        if (!Networking.IsHost) return;
        
        RoundEndTime = 10f;
    }

    protected override void OnUpdate()
    {
        if (Networking.IsHost)
        {
            if (RoundEndTime)
            {
	            AssignPlayersToTeamsAndSpawnpoints();
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

    private void AssignPlayersToTeamsAndSpawnpoints()
    {
        var players = SmashCannon.Players.ToList();
        var random = new Random();

        if (players.Count < 1)
        {
            throw new Exception("Not enough players to assign to spawn points");
        }

        players = players.OrderBy(x => random.Next()).ToList();

        var cannonPlayerCount = players.Count > 3 ? 2 : 1;

        for (var i = 0; i < cannonPlayerCount; i++)
        {
            var player = players[i];
            player.TeamCategory = new SmashTeam();
            player.Transform.LocalPosition = AssignCannonSpawnpoint(random);
        }

        // Assign runner players
        for (var i = cannonPlayerCount; i < players.Count; i++)
        {
            var player = players[i];
            player.TeamCategory = new RunnerTeam();
            player.Transform.LocalPosition = AssignRunnerSpawnpoint(random);
        }
    }

    private Vector3 AssignCannonSpawnpoint(Random random)
    {
        var cannonSpawns = CannonSpawnPoint.ToList();

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

        return selectedSpawn.Transform.World.Position;
    }

    private Vector3 AssignRunnerSpawnpoint(Random random)
    {
        var runnerSpawns = RunnerSpawnPoint.ToList();

        if (runnerSpawns.Count == 0)
        {
            throw new Exception("No runner spawnpoints available");
        }

        int randomIndex = random.Next(runnerSpawns.Count);
        var selectedSpawn = runnerSpawns[randomIndex];

        return selectedSpawn.Transform.World.Position;
    }
}
