using Sandbox;
using Sandbox.Diagnostics;
using Sandbox.Services;
using System;
using System.Threading;

public sealed class CannonComponent : Component
{
	[Property] GameObject Gun { get; set; }
	[Property] GameObject Bullet { get; set; }
	[Property] GameObject Muzzle { get; set; }
	[Property] GameObject GunModel { get; set; }
	[Property][Category( "Turret" )][Range( 0f, 1, 0.000001f )]public float TurretYawSpeed { get; set; } = 0.2f;
	[Property][Category( "Turret" )][Range( 0f, 1, 0.000001f )]public float TurretPitchSpeed { get; set; } = 1f;

	float turretYaw;
	float turretPitch;

	TimeSince timeSinceLastPrimary = 10;
	protected override void OnUpdate()
	{
		// rotate gun using mouse input
		
		

		if(Input.Down("Left") || Input.Down("Right")) {
  		var yawValue = Input.Down("Left") ? TurretYawSpeed: (Input.Down("Right") ? -TurretYawSpeed : 0f);
		turretYaw += yawValue;
		}

		if(Input.Down("Forward") || Input.Down("Backward")) {
  		var pitchValue = Input.Down("Forward") ? TurretPitchSpeed: (Input.Down("Backward") ? -TurretPitchSpeed : 0f);
		turretPitch += pitchValue;
		}

		turretPitch = turretPitch.Clamp( -30, 30 );
		Gun.Transform.Rotation = Rotation.From( turretPitch, turretYaw, 0 );

		if ( Input.Pressed( "Attack1" ) && timeSinceLastPrimary > 3.0f)
		{
			Assert.NotNull( Bullet );

			var obj = Bullet.Clone( Muzzle.Transform.Position, Muzzle.Transform.Rotation );
			var physics = obj.Components.Get<Rigidbody>( FindMode.EnabledInSelfAndDescendants );
			if ( physics is not null )
			{
				physics.Velocity = Muzzle.Transform.Rotation.Forward * 2000.0f;
			}

			Stats.Increment( "balls_fired", 1 );

			// Testing sound
			Sound.Play( "sounds/kenney/ui/ui.downvote.sound", Transform.Position );
			timeSinceLastPrimary = 0;
		}
	}
}
