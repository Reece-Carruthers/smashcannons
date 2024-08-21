public sealed class BoxForce : Component, Component.ICollisionListener
{
    public void OnCollisionStart( Collision collision )
    {
        if (!collision.Other.GameObject.Tags.Has( "player" ) ) return;
        
        var player = collision.Other.GameObject.Components.Get<SmashRunnerMovement>();

        if ( player is null ) return;
        
        player.Kill();

    }
}
