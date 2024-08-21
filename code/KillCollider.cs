using Sandbox;

public sealed class KillCollider : Component, Component.ITriggerListener
{

	public void OnTriggerEnter( Collider other )
	{
		var obj = other.GameObject;

		if ( !obj.IsValid() ) return;
		
		HandlePlayerCollision( obj );
	}

	private void HandlePlayerCollision( GameObject other )
	{
		if ( !other.Tags.Has( "player" ) ) return;

		var playerScript = other.Components.Get<SmashRunnerMovement>();

		if ( playerScript is null ) return;

		playerScript.Kill();
	}
}
