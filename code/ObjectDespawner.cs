using Sandbox;

public sealed class ObjectDespawner : Component, Component.ITriggerListener
{
	public void OnTriggerEnter( Collider other )
	{
		var obj = other.GameObject;

		if ( !obj.IsValid() ) return;

		HandleMapItemCollision( obj );
	}

	private void HandleMapItemCollision( GameObject other )
	{
		if ( !other.Tags.Has( "map_item" ) ) return;

		if ( !other.IsValid() ) return;

		var selfDestruct = other.Components.Create<SelfDestructComponent>( false );

		selfDestruct.Seconds = 3f;
		selfDestruct.Enabled = true;
	}
}
