using Sandbox.Diagnostics;
using Sandbox.Services;

public sealed class CannonComponent : Component
{
	[Property] GameObject Gun { get; set; }
	[Property] GameObject Bullet { get; set; }
	[Property] GameObject Muzzle { get; set; }

	[Property]
	[Category( "Turret" )]
	[Range( 0f, 1, 0.000001f )]
	public float TurretYawSpeed { get; set; } = 250f;

	[Property]
	[Category( "Turret" )]
	[Range( 0f, 1, 0.000001f )]
	public float TurretPitchSpeed { get; set; } = 1000f;

	float turretYaw;
	float turretPitch;

	TimeSince timeSinceLastPrimary = 10;

	public Connection CurrentController { get; set; } = null;
	

	 protected override void OnFixedUpdate()
	{
		if ( Network.IsProxy && !Network.IsOwner) return;

		if ( CurrentController != Network.OwnerConnection || CurrentController is null) return;

		if ( Input.Down( "Left" ) || Input.Down( "Right" ) )
		{
			MoveYaw();
		}

		if ( Input.Down( "Forward" ) || Input.Down( "Backward" ) )
		{
			MovePitch();
		}

		turretPitch = turretPitch.Clamp( -30, 30 );
		turretYaw = turretYaw.Clamp( -70, 70 );
		Gun.Transform.Rotation = Rotation.From( turretPitch, turretYaw, 0 );

		if ( Input.Pressed( "Attack1" ) && timeSinceLastPrimary > 3.0f )
		{
			Shoot();
		}

	}

	private void MoveYaw()
	{
		var yawValue = Input.Down( "Left" ) ? TurretYawSpeed : (Input.Down( "Right" ) ? -TurretYawSpeed : 0f);
		turretYaw += yawValue;
	}
	
	private void MovePitch()
	{
		var pitchValue = Input.Down( "Forward" )
			? TurretPitchSpeed
			: (Input.Down( "Backward" ) ? -TurretPitchSpeed : 0f);
		turretPitch += pitchValue;
	}

	private void Shoot()
	{
		Assert.NotNull( Bullet );

		var obj = Bullet.Clone( Muzzle.Transform.Position, Muzzle.Transform.Rotation );
		
		var physics = obj.Components.Get<Rigidbody>( FindMode.EnabledInSelfAndDescendants );

		if ( physics is null )
		{
			return;
		}

		obj.NetworkSpawn();
		physics.Velocity = Muzzle.Transform.Rotation.Forward * 2000.0f;

		Stats.Increment( "balls_fired", 1 );
		PlaySound("cannonshot");
		timeSinceLastPrimary = 0;
	}
	
	[Broadcast]
	private void PlaySound( string soundName )
	{
		Sound.Play( soundName, Transform.Position );
	}
}
