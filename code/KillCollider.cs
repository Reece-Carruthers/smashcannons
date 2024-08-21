using Sandbox;

public sealed class KillCollider : Component, Component.ITriggerListener
{
	private List<SmashRunnerMovement> DeadPlayers = new List<SmashRunnerMovement>();

	public void OnTriggerEnter( Collider other )
	{
		var player = other.GameObject;
		if ( !player.Tags.Has( "player" ) ) return;

		var playerScript = player.Components.Get<SmashRunnerMovement>();

		if ( playerScript is null ) return;

		DeadPlayers.Add( playerScript );
		playerScript.DeathTimer = 3f;
		playerScript.Kill();
	}

	protected override void OnUpdate()
	{
		RespawnAsSpectator();
	}

	private void RespawnAsSpectator()
	{
		if ( Networking.IsHost )
		{
			foreach ( var deadPlayer in DeadPlayers.ToList() )
			{
				if ( deadPlayer.DeathTimer && deadPlayer.LifeState == LifeState.Dead )
				{
					Log.Info( "INSIDE LOOP" );
					deadPlayer.HandleSpectate();
					DeadPlayers.Remove( deadPlayer );
				}
			}
		}
	}
}
