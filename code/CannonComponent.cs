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

	TimeSince timeSinceLastPrimary = 10;

	public Connection CurrentController { get; set; } = null;
	public SmashRunnerMovement CurrentPlayer { get; set; } = null;

	public float TimeBetweenShots = 5.0f;


	protected override void OnFixedUpdate()
	{
		if ( Network.IsProxy && !Network.IsOwner ) return;

		if ( CurrentController != Network.OwnerConnection || CurrentController is null ) return;

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
		Gun.Transform.Rotation = Rotation.From( turretPitch, turretYaw, 0 );

		if ( Input.Pressed( "Attack1" ) && timeSinceLastPrimary > 5.0f )
		{
			var activeState = StateSystem.Active as FinalState;

			if ( activeState.IsValid() )
			{
				TimeBetweenShots = 3.0f;
			}
			else
			{
				TimeBetweenShots = 5.0f;
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

	private void Shoot()
	{
		Assert.NotNull( Bullet );

		var obj = Bullet.Clone( Muzzle.Transform.Position, Muzzle.Transform.Rotation );

		var physics = obj.Components.Get<Rigidbody>();

		if ( physics is null ) return;

		obj.NetworkSpawn();
		physics.Velocity = Muzzle.Transform.Rotation.Forward * 2000.0f;

		Stats.Increment("cannon_ball_fired", 1 );
		
		PlaySound( "cannonshot" );
		timeSinceLastPrimary = 0;
	}

	[Broadcast]
	private void PlaySound( string soundName )
	{
		Sound.Play( soundName, Transform.World.Position );
	}
}
