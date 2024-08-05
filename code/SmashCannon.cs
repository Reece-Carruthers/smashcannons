using Sandbox;
using Sandbox.Network;

public sealed class SmashCannon : Component, Component.INetworkListener
{
	
	public static SmashCannon Instance { get; private set; }
	
	protected override void OnAwake()
	{
		Instance = this;
		base.OnAwake();
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
			state.RoundEndTime = 5f;
		}
		base.OnStart();
	}
}
