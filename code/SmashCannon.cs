using Sandbox;
using Sandbox.Network;

public sealed class SmashCannon : Component, Component.INetworkListener
{
	public static IEnumerable<SmashRunnerMovement> Players => InternalPlayers.Where(p => p.IsValid());

	private static int MaxPlayers = 64;
	private static List<SmashRunnerMovement> InternalPlayers { get; set; } = Enumerable.Repeat<SmashRunnerMovement>(null, MaxPlayers).ToList();
	
	private static  IEnumerable<PlayerSpawn> SpawnPoints { get; set; }
	public static IEnumerable<PlayerSpawn> RunnerSpawnPoint =>
		SpawnPoints.Where(x => x.Tags.Has("runner_spawn"));
	public static IEnumerable<PlayerSpawn> CannonSpawnPoint =>
		SpawnPoints.Where(x => x.Tags.Has("cannon_spawn"));

	[Property] public GameObject PlayerPrefab { get; set; }
	public static SmashCannon Instance { get; private set; }
	
	protected override void OnAwake()
	{
		Instance = this;
		base.OnAwake();
	}

	public static void AddPlayer(int slot, SmashRunnerMovement player)
	{
		player.PlayerSlot = slot;
		InternalPlayers[slot] = player;
	}

	private int FindFreeSlot()
	{
		for (var i = 0; i < MaxPlayers; i++)
		{
			var player = InternalPlayers[i];
			if (player.IsValid()) continue;
			return i;
		}

		return -1;
	}

	protected override void OnStart()
	{
		if (!GameNetworkSystem.IsActive)
		{
			GameNetworkSystem.CreateLobby();
			Log.Info("Lobby Created");

		}

		if (Networking.IsHost)
		{
			var state = StateSystem.Set<LobbyState>();
			state.RoundEndTime = 60f;
		}
		base.OnStart();
	}

	void INetworkListener.OnActive(Connection connection)
	{
		SpawnPoints = Scene.Components.GetAll<PlayerSpawn>();

		var player = PlayerPrefab.Clone();
		var playerSlot = FindFreeSlot();

		if (playerSlot < 0)
		{
			throw new("Player joined but there's no free slots!");
		}

		var playerComponent = player.Components.Get<SmashRunnerMovement>();
		AddPlayer(playerSlot, playerComponent);

		player.Transform.LocalPosition = RunnerSpawnPoint.First().Transform.Position;
		player.NetworkSpawn(connection);
	}
}
