using System;
using Sandbox;
using Sandbox.Citizen;


public sealed class SmashRunnerMovement : Component
{
	[Category( "Movement Properties" )]
	[Property]
	public float GroundControl { get; set; } = 4.0f;

	[Category( "Movement Properties" )]
	[Property]
	public float AirControl { get; set; } = 0.1f;

	[Category( "Movement Properties" )]
	[Property]
	public float MaxForce { get; set; } = 50f;

	[Category( "Movement Properties" )]
	[Property]
	public float Speed { get; set; } = 160f;

	[Category( "Movement Properties" )]
	[Property]
	public float RunSpeed { get; set; } = 290f;

	[Category( "Movement Properties" )]
	[Property]
	public float CrouchSpeed { get; set; } = 80f;

	[Category( "Movement Properties" )]
	[Property]
	public float JumpForce { get; set; } = 400f;

	[Category( "Objects" )] [Property] public GameObject Head { get; set; }
	[Category( "Objects" )] [Property] public GameObject Body { get; set; }

	public bool IsCrouching = false;
	public bool IsSprinting = false;

	public CharacterController characterController;
	private CitizenAnimationHelper animationHelper;

	Vector3 WishVelocity = Vector3.Zero;


	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		animationHelper = Components.Get<CitizenAnimationHelper>();
	}

	protected override void OnUpdate()
	{
		UpdateCrouch();
		IsSprinting = Input.Down( "Run" );
		if ( Input.Pressed( "jump" ) )
		{
			Jump();
		}
		
		RotateBody();
		UpdateAnimations();
	}

	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		Move();
	}

	void Move()
	{
		var gravity = Scene.PhysicsWorld.Gravity;

		if ( characterController.IsOnGround )
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate( WishVelocity );
			characterController.ApplyFriction( GroundControl );
		}
		else
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate( WishVelocity.ClampLength( MaxForce ) );
			characterController.ApplyFriction( AirControl );
		}

		characterController.Move();

		if ( !characterController.IsOnGround )
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
		}
	}

	void BuildWishVelocity()
	{
		WishVelocity = 0;

		var rot = Head.Transform.Rotation;
		if ( Input.Down( "Forward" ) ) WishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += rot.Right;

		WishVelocity = WishVelocity.WithZ( 0 );

		if ( !WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;

		if ( IsCrouching ) WishVelocity *= CrouchSpeed;
		else if ( IsSprinting ) WishVelocity *= RunSpeed;
		else WishVelocity *= Speed;
	}

	void RotateBody()
	{
		if ( Body is null ) return;

		var targetAngle = new Angles( 0, Head.Transform.Rotation.Yaw(), 0 ).ToRotation();
		float rotateDifference = Body.Transform.Rotation.Distance( targetAngle );

		if ( rotateDifference > 50f || characterController.Velocity.Length > 10f )
		{
			Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, targetAngle, Time.Delta * 2f );
		}
	}

	void Jump()
	{
		if ( !characterController.IsOnGround ) return;

		characterController.Punch( Vector3.Up * JumpForce );
		animationHelper.TriggerJump();
	}

	void UpdateAnimations()
	{
		if ( animationHelper is null ) return;

		animationHelper.WithWishVelocity( WishVelocity );
		animationHelper.WithVelocity( characterController.Velocity );
		animationHelper.AimAngle = Head.Transform.Rotation;
		animationHelper.IsGrounded = characterController.IsOnGround;
		animationHelper.WithLook( Head.Transform.Rotation.Forward, 1f, 0.75f, 0f );
		animationHelper.MoveStyle =
			IsSprinting ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
		animationHelper.DuckLevel = IsCrouching ? 1f : 0f;
	}

	void UpdateCrouch()
	{
		if ( characterController is null ) return;

		if ( Input.Pressed( "Duck" ) && !IsCrouching )
		{
			IsCrouching = true;
			characterController.Height /= 2f;
		}

		if ( !Input.Down( "Duck" ) && IsCrouching && !IsObjectAbove() )
		{
			IsCrouching = false;
			characterController.Height *= 2f;
		}
	}

	bool IsObjectAbove()
	{
		var crouchedHeight = characterController.Height;
		var standingHeight = crouchedHeight * 2;
		var traceHeight = standingHeight - crouchedHeight;

		var traceStart = characterController.Transform.Position + Vector3.Up * crouchedHeight;
		var traceEnd = traceStart + Vector3.Up * traceHeight;


		var playerTrace = Scene.Trace.Ray( traceStart, traceEnd )
			.Size( characterController.BoundingBox )
			.WithoutTags( "player", "trigger" )
			.UseHitboxes()
			.Run();

		return playerTrace.Hit;
	}
}
