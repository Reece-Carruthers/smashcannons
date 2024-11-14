using Sandbox;
using Sandbox.Network;

public sealed class SmashCannon : Component, Component.INetworkListener
{
	public static IEnumerable<SmashRunnerMovement> Players => InternalPlayers.Where( p => p.IsValid() );

	private static int MaxPlayers = 64;

	private static List<SmashRunnerMovement> InternalPlayers { get; set; } =
		Enumerable.Repeat<SmashRunnerMovement>( null, MaxPlayers ).ToList();

	private static IEnumerable<PlayerSpawn> SpawnPoints { get; set; }

	public static IEnumerable<PlayerSpawn> RunnerSpawnPoint =>
		SpawnPoints.Where( x => x.Tags.Has( "runner_spawn" ) );

	public static IEnumerable<PlayerSpawn> CannonSpawnPoint =>
		SpawnPoints.Where( x => x.Tags.Has( "cannon_spawn" ) );

	private static IEnumerable<PlayerSpawn> DeadSpawnPoint =>
		SpawnPoints.Where( x => x.Tags.Has( "dead_spawn" ) );

	[Property] public GameObject Platforms { get; set; }
	[Property] public GameObject Pillars { get; set; }
	[Property] public GameObject Ramp { get; set; }
	
	[Property] public GameObject NetworkedRampObjects { get; set; }
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject KillButton { get; set; }

	public static SmashCannon Instance { get; private set; }

	protected override void OnAwake()
	{
		Instance = this;
		base.OnAwake();
	}

	public static void AddPlayer( int slot, SmashRunnerMovement player )
	{
		player.PlayerSlot = slot;
		InternalPlayers[slot] = player;
	}

	private int FindFreeSlot()
	{
		for ( var i = 0; i < MaxPlayers; i++ )
		{
			var player = InternalPlayers[i];
			if ( player.IsValid() ) continue;
			return i;
		}

		return -1;
	}

	protected override void OnStart()
	{
		if ( !Networking.IsActive )
		{
			Networking.CreateLobby();
		}

		if ( Networking.IsHost )
		{
			StateSystem.Set<LobbyState>();
		}

		base.OnStart();
	}

	void INetworkListener.OnActive( Connection connection )
	{
		SpawnPoints = Scene.Components.GetAll<PlayerSpawn>();

		var player = PlayerPrefab.Clone();
		var playerSlot = FindFreeSlot();

		if ( playerSlot < 0 )
		{
			Log.Warning( "No available player slots" );
		}


		var activeState = StateSystem.Active as LobbyState;
		var playerComponent = player.Components.Get<SmashRunnerMovement>();

		AddPlayer( playerSlot, playerComponent );
		player.LocalPosition = DeadSpawnPoint.First().WorldPosition;

		if ( !activeState.IsValid() )
		{
			playerComponent.LifeState = LifeState.Spectate;
		}

		player.NetworkSpawn( connection );
	}
}
