using System;
using cba.smashcannons;
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
	private float JumpForce { get; set; } = 400f;

	[Category( "Objects" )] [Property] public GameObject Head { get; set; }
	[Category( "Objects" )] [Property] public GameObject Body { get; set; }

	[HostSync] public LifeState LifeState { get; set; } = LifeState.Alive;

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

	[HostSync] public TimeUntil DeathTimer { get; set; }
	[HostSync] public bool DeathTimerAssigned { get; set; } = false;

	[Property] public RagdollController RagdollController { get; set; }

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
		if ( cannon is not null &&
		     cannon.Network.OwnerConnection !=
		     Network.OwnerConnection )
		{
			isControllingCannon = false;
		}

		if ( !isControllingCannon )
		{
			if ( !Network.IsProxy )
			{
				IsSprinting = Input.Down( "Run" );
				if ( Input.Pressed( "jump" ) )
				{
					Jump();
				}

				if ( Input.Pressed( "Use" ) && TeamCategory.CanControlCannon() )
				{
					TryTakeCannon();
				}

				if ( Input.Pressed( "Use" ) )
				{
					TryKillCannons();
				}

				TargetAngle = new Angles( 0, Head.Transform.Rotation.Yaw(), 0 ).ToRotation();
			}

			RotateBody();
			UpdateAnimations();
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( LifeState == LifeState.Dead ) return;
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

		if ( IsSprinting ) WishVelocity *= RunSpeed;
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
		cannonComponent.CurrentPlayer = this;

		isControllingCannon = true;
		cannon = cannonComponent;
		characterController.Velocity = Vector3.Zero;
	}

	private void TryKillCannons()
	{
		var position = Head.Transform.Position.WithZ( Head.Transform.Position.z + 50f );

		var tr = Scene.Trace.WithoutTags( "player" )
			.Sphere( 32, Head.Transform.Position,
				position )
			.Run();

		if ( !tr.Hit || !tr.GameObject.Tags.Has( "button" ) ) return;


		var killButton = tr.GameObject.Components.Get<KillButton>();


		if ( killButton is null ) return;


		killButton.Kill( this );
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

	[Broadcast( NetPermission.HostOnly )]
	public void Respawn()
	{
		MoveToSpawnpoint();

		if ( Networking.IsHost )
		{
			RagdollController.Unragdoll();
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
		if ( IsProxy ) return;


		if ( TeamCategory is RunnerTeam )
		{
			Transform.Position = AssignRunnerSpawnPoint();
		}

		if ( TeamCategory is SmashTeam )
		{
			if ( CannonSpawnpoint == new Vector3( 0 ) ) return;

			Transform.Position = CannonSpawnpoint;
		}
	}

	[Broadcast( NetPermission.HostOnly )]
	public void Teleport( Vector3 location )
	{
		if ( IsProxy ) return;

		Transform.Position = location;
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

	[Broadcast( NetPermission.HostOnly )]
	public void HandleSpectate()
	{
		var deadSpawn = Scene.Components.GetAll<PlayerSpawn>().FirstOrDefault( x => x.Tags.Has( "dead_spawn" ) );

		if ( deadSpawn is null ) return;

		UnragdollPlayer();

		characterController.Velocity = 0;

		Body.Transform.LocalPosition = Vector3.Zero;

		Teleport( deadSpawn.Transform.Position );

		LifeState = LifeState.Spectate;
	}

	public void Kill( bool withImpulse = false )
	{
		if ( LifeState == LifeState.Dead ) return;
		LifeState = LifeState.Dead;

		var playerPosition = Transform.Position;

		var direction = Vector3.Up +
		                new Vector3( Game.Random.Float( -0.25f, 0.25f ), Game.Random.Float( -0.25f, 0.25f ), 0f );

		if ( withImpulse )
		{
			RagdollPlayerWithImpulse( playerPosition, direction );
		}
		else
		{
			RagdollPlayer( playerPosition, direction );
		}

		DeathMessage();
		PlayDeathNoise();

	}

	[Broadcast(NetPermission.HostOnly)]
	private void DeathMessage()
	{
		if ( !Networking.IsHost ) return;
		
		Chat.AddPlayerEvent( "dead", Network.OwnerConnection.DisplayName, TeamCategory.Colour(),
			$"has been killed" );
	}

	[Broadcast]
	private void PlayDeathNoise()
	{
		Sound.Play( "dead", Transform.World.Position );
	}

	[Broadcast( NetPermission.HostOnly )]
	private void RagdollPlayer( Vector3 playerPosition, Vector3 direction )
	{
		RagdollController.Ragdoll( playerPosition, direction );
	}

	[Broadcast( NetPermission.HostOnly )]
	private void RagdollPlayerWithImpulse( Vector3 playerPosition, Vector3 direction )
	{
		RagdollController.RagdollWithImpulse( playerPosition, direction );
	}

	[Broadcast( NetPermission.HostOnly )]
	private void UnragdollPlayer()
	{
		RagdollController.Unragdoll();
	}

	public void AddStat( string statId )
	{
		if ( Network.IsProxy ) return;

		Sandbox.Services.Stats.Increment( statId, 1 );
	}
}
