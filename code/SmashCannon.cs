using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.Network;

public sealed class SmashCannon : Component, Component.INetworkListener
{
	public static IEnumerable<SmashRunnerMovement> Players => InternalPlayers.Where(p => p.IsValid());
	private static List<SmashRunnerMovement> InternalPlayers { get; set; } = new(4) { null, null, null, null };

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
		for (var i = 0; i < 4; i++)
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
			state.RoundEndTime = 30f;
		}
		base.OnStart();
	}

	void INetworkListener.OnActive(Connection connection)
	{

		var player = PlayerPrefab.Clone();
		var playerSlot = FindFreeSlot();

		if (playerSlot < 0)
		{
			throw new("Player joined but there's no free slots!");
		}

		var playerComponent = player.Components.Get<SmashRunnerMovement>();
		AddPlayer(playerSlot, playerComponent);

		player.Transform.LocalPosition = Scene.Components.GetAll<PlayerSpawn>().First().Transform.World.Position;
		player.NetworkSpawn(connection);
	}
}
