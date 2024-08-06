using Sandbox;

public sealed class KillCollider : Component, Component.ITriggerListener
{
	
	public void OnTriggerEnter( Collider other )
	{
		var player = other.GameObject;
		if ( !player .Tags.Has("player") ) return;

		var playerScript = player.Components.Get<SmashRunnerMovement>();
		if ( playerScript is null ) return;
		playerScript.Kill();
	}
}

