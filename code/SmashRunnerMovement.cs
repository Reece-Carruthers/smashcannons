using System;
using Microsoft.VisualBasic;
using Sandbox;
using Sandbox.Citizen;


public sealed class SmashRunnerMovement : Component
{
	[HostSync] public int PlayerSlot { get; set; }
	[HostSync] public Team TeamCategory { get; set; } = new RunnerTeam();

	[Category( "Movement Properties" )]
	[Property]
	private float GroundControl { get; set; } = 4.0f;

	[Category( "Movement Properties" )]
	[Property]
	private float AirControl { get; set; } = 0.1f;

	[Category( "Movement Properties" )]
	[Property]
	private float MaxForce { get; set; } = 50f;

	[Category( "Movement Properties" )]
	[Property]
	private float Speed { get; set; } = 160f;

	[Category( "Movement Properties" )]
	[Property]
	private float RunSpeed { get; set; } = 290f;

	[Category( "Movement Properties" )]
	[Property]
	private float CrouchSpeed { get; set; } = 80f;

	[Category( "Movement Properties" )]
	[Property]
	private float JumpForce { get; set; } = 400f;

	[Category( "Objects" )] [Property] public GameObject Head { get; set; }
	[Category( "Objects" )] [Property] public GameObject Body { get; set; }

	[HostSync] public LifeState LifeState { get; set; } = LifeState.Alive;

	[Sync] public bool IsCrouching { get; set; } = false;
	[Sync] private bool IsSprinting { get; set; } = false;
	[Sync] Angles TargetAngle { get; set; } = Angles.Zero;

	[Sync] private ClothingContainer clothingEntry { get; set; } = null;

	public CharacterController characterController;
	private CitizenAnimationHelper animationHelper;
	private SkinnedModelRenderer BodyRenderer { get; set; }

	Vector3 WishVelocity = Vector3.Zero;

	private bool isControllingCannon { get; set; } = false;
	private CannonComponent cannon { get; set; } = null;

	[Property] Collider playerCollider { get; set; }
	[HostSync] public Vector3 CannonSpawnpoint { get; set; }

	public static SmashRunnerMovement Local
	{
		get
		{
			if ( !_local.IsValid() )
			{
				_local = Game.ActiveScene.GetAllComponents<SmashRunnerMovement>()
					.FirstOrDefault( x => x.Network.IsOwner );
			}

			return _local;
		}
	}

	private static SmashRunnerMovement _local = null;


	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		animationHelper = Components.Get<CitizenAnimationHelper>();
		BodyRenderer = Body.Components.Get<SkinnedModelRenderer>();
	}

	protected override void OnUpdate()
	{
		if ( LifeState == LifeState.Dead )
		{
			// If the player is dead, skip all update logic
			return;
		}

		if ( cannon is not null &&
		     cannon.Network.OwnerConnection !=
		     Network.OwnerConnection ) //TODO: Do we need a is proxy check before setting this?
		{
			isControllingCannon = false;
		}

		if ( !isControllingCannon )
		{
			if ( !Network.IsProxy )
			{
				UpdateCrouch();
				IsSprinting = Input.Down( "Run" );
				if ( Input.Pressed( "jump" ) )
				{
					Jump();
				}

				if ( Input.Pressed( "Use" ) && TeamCategory.CanControlCannon() )
				{
					TryTakeCannon();
				}

				TargetAngle = new Angles( 0, Head.Transform.Rotation.Yaw(), 0 ).ToRotation();
			}

			RotateBody();
			UpdateAnimations();
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( LifeState == LifeState.Dead )
		{
			// If the player is dead, skip all update logic
			return;
		}

		if ( Network.IsProxy ) return;
		if ( isControllingCannon ) return;

		BuildWishVelocity();
		Move();
	}

	private void Move()
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

	private void BuildWishVelocity()
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

	private void RotateBody()
	{
		if ( Body is null ) return;

		var rotateDifference = Body.Transform.Rotation.Distance( TargetAngle );

		if ( rotateDifference > 50f || characterController.Velocity.Length > 10f )
		{
			Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, TargetAngle, Time.Delta * 4f );
		}
	}

	private void Jump()
	{
		if ( !characterController.IsOnGround ) return;
		characterController.Punch( Vector3.Up * JumpForce );
		BroadcastJumpAnimation();
	}

	private void UpdateAnimations()
	{
		if ( Network.IsProxy )
		{
			RenderModelAndClothes();
		}

		if ( animationHelper is null ) return;

		animationHelper.WithWishVelocity( WishVelocity );
		animationHelper.WithVelocity( characterController.Velocity );
		animationHelper.AimAngle = TargetAngle;
		animationHelper.IsGrounded = characterController.IsOnGround;
		animationHelper.WithLook( TargetAngle.Forward, 1f, 0.75f, 0f );
		animationHelper.MoveStyle =
			IsSprinting ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
		animationHelper.DuckLevel = IsCrouching ? 1f : 0f;
	}

	private void UpdateCrouch()
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

	private bool IsObjectAbove()
	{
		var crouchedHeight = characterController.Height / 2;
		var tr = characterController.TraceDirection( Vector3.Up * crouchedHeight );
		return tr.Hit;
	}

	[Broadcast]
	private void BroadcastJumpAnimation()
	{
		animationHelper?.TriggerJump();
	}

	private void TryTakeCannon()
	{
		var position = Head.Transform.Position.WithZ( Head.Transform.Position.z + 50f );

		var tr = Scene.Trace.WithoutTags( "player" )
			.Sphere( 32, position,
				position )
			.Run();

		if ( !tr.Hit || !tr.GameObject.Tags.Has( "cannon_zone" ) ) return;

		var cannonComponent = tr.GameObject.Components.Get<CannonComponent>();

		if ( cannonComponent is null ) return;

		var takeOverResult = tr.GameObject.Network.TakeOwnership();

		if ( !takeOverResult )
		{
			return;
		}

		cannonComponent.CurrentController = Network.OwnerConnection;
		isControllingCannon = true;
		cannon = cannonComponent;
		characterController.Velocity = Vector3.Zero;
	}

	private void RenderModelAndClothes()
	{
		BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;

		var clothingList = Body.Components.GetAll<SkinnedModelRenderer>( FindMode.EverythingInDescendants )
			.Where( x => x.Tags.Has( "clothing" ) );
		foreach ( var clothing in clothingList )
		{
			clothing.RenderType = ModelRenderer.ShadowRenderType.On;
		}
	}

	public void Kill() //TODO: If lobby state disable kill
	{
		LifeState = LifeState.Dead;

		var ragdollController = Components.Get<RagdollController>();
		if ( ragdollController is null ) return;

		var playerPosition = Transform.Position;

		var direction = Vector3.Up +
		                new Vector3( Game.Random.Float( -0.25f, 0.25f ), Game.Random.Float( -0.25f, 0.25f ), 0f );
		ragdollController.Ragdoll( playerPosition, direction );
	}

	[Broadcast( NetPermission.HostOnly )]
	public void Respawn()
	{
		MoveToSpawnpoint();

		if ( Networking.IsHost )
		{
			var ragdollController = Components.Get<RagdollController>();
			if ( ragdollController is null ) return;

			ragdollController.Unragdoll();
			LifeState = LifeState.Alive;
		}
	}

	public void UpdateTeam( Team team )
	{
		if ( Networking.IsHost )
		{
			TeamCategory = team;
		}
	}

	public void MoveToSpawnpoint()
	{
		Teleport();
	}

	public void Teleport()
	{
		if ( IsProxy ) return;


		if ( TeamCategory is RunnerTeam )
		{
			Transform.Position = AssignRunnerSpawnPoint();
		}

		if ( TeamCategory is SmashTeam )
		{
			if ( CannonSpawnpoint == new Vector3( 0 ) )
			{
				return;
			}

			Transform.Position = CannonSpawnpoint;
		}
	}
	
	private Vector3 AssignRunnerSpawnPoint()
	{
		var random = new Random();
		var runnerSpawns = Game.ActiveScene.Components.GetAll<PlayerSpawn>().Where( x => x.Tags.Has( "runner_spawn" ) )
			.ToList();

		var randomIndex = random.Next( runnerSpawns.Count );
		var selectedSpawn = runnerSpawns[randomIndex];

		return selectedSpawn.Transform.Position;
	}
}
