using Sandbox.Diagnostics;
using Sandbox.Services;

public sealed class CannonComponent : Component
{
	[Property] GameObject Gun { get; set; }
	[Property] GameObject Bullet { get; set; }
	[Property] GameObject Muzzle { get; set; }

	private float TurretYawSpeed { get; set; } = 0.25f;
	private float TurretPitchSpeed { get; set; } = 0.5f;

	float turretYaw;
	float turretPitch;
	
	public Connection CurrentController { get; set; } = null;
	public SmashRunnerMovement CurrentPlayer { get; set; } = null;

	public float TimeBetweenShots = 5.0f;

	public float CannonForce = 2200.0f;
	
	[HostSync] public TimeSince timeSinceLastPrimary { get; set; } = 10;


	protected override void OnFixedUpdate()
	{
		if ( Network.IsProxy && !Network.IsOwner ) return;

		if ( CurrentController != Network.Owner || CurrentController is null ) return;

		if ( CurrentPlayer.LifeState is LifeState.Dead or LifeState.Spectate ) return;

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
		Gun.WorldRotation = Rotation.From( turretPitch, turretYaw, 0 );

		if ( Input.Pressed( "Attack1" ))
		{
			var activeState = StateSystem.Active as FinalState;

			if ( activeState.IsValid() )
			{
				TimeBetweenShots = 2.0f;
			}
			else
			{
				TimeBetweenShots = 6.0f;
			}

			if ( timeSinceLastPrimary > TimeBetweenShots )
			{
				Shoot();
			}
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

	[Broadcast(NetPermission.Anyone)]
	private void Shoot()
	{
		if ( !Networking.IsHost ) return; // Make the host shoot the cannon balls, work around for weird networking issue here clients cannon balls were not hurting the platforms, made timeSinceLastPrimary a host sync to allow it to work with this method
		
		Assert.NotNull( Bullet );

		var obj = Bullet.Clone( Muzzle.WorldPosition, Muzzle.WorldRotation );

		var physics = obj.Components.Get<Rigidbody>();

		if ( physics is null ) return;

		obj.NetworkSpawn();
		physics.Velocity = Muzzle.WorldRotation.Forward * CannonForce;
		
		PlaySound( "cannonshot" );
		timeSinceLastPrimary = 0;
	}

	[Broadcast]
	private void PlaySound( string soundName )
	{
		Sound.Play( soundName, Transform.World.Position );
	}
}
