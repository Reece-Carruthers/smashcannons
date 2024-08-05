public sealed class BoxForce : Component, Component.ICollisionListener
{
    public float BounceForce = 50000f;

    public void OnCollisionStart( Collision collision )
    {
        Log.Info( "OUTSIDE" );

        if (!collision.Other.GameObject.Tags.Has( "player" ) ) return;

        Log.Info( "HERE" );

        var player = collision.Other.Body;

        // var normal = collision.Contact.Normal;

        Vector3 bounceDirection = (Transform.Position - player.Position).Normal;

        // player.Position = new Vector3( 0, 0, 0 );

        player.ApplyForce(bounceDirection * BounceForce);

        // player.ApplyForce( new Vector3( 0, 0, 0f ).WithZ( BounceForce ) );
    }
}
