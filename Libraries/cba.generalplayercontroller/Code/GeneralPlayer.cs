using Sandbox;
using Sandbox.Citizen;

/// <summary>
/// A General Player component that handles the player's movement, camera, and animations.
/// The plauer is in third-person view, over the right shoulder by default.
/// </summary>
public sealed class GeneralPlayerController : Component
{
	[Property]
	[Category( "Components" )]
	public GameObject Camera { get; set; }

	[Property]
	[Category( "Components" )]

	public CharacterController Controller { get; set; }

	[Property]
	[Category( "Components" )]
	public CitizenAnimationHelper Animator { get; set; }

	/// <summary>
	/// How fast the player can walk (in units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 400f, 1f )]
	public float WalkSpeed { get; set; } = 200f;

	/// <summary>
	/// How fast the player can crouch walk (in units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 400f, 1f )]
	public float CrouchSpeed { get; set; } = 120f;

	/// <summary>
	/// How fast the player can run (in units per second)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 800, 1f )]
	public float RunSpeed { get; set; } = 300f;

	/// <summary>
	/// How high the player can jump (in units)
	/// </summary>
	/// <value></value>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1000, 10f )]
	public float JumpHeight { get; set; } = 300f;


	/// <summary>
	/// Where the camera roates around the player and the aim originates from
	/// </summary>
	[Property]
	public Vector3 EyePosition { get; set; }

	public Angles EyeAngles { get; set; }
	Transform _initialCameraTransform;

	/// <summary>
	/// Where the camera starts when the player is created
	/// </summary>
	[Property]
	[Category( "Camera" )]
	public Vector3 InitialCameraPosition { get; set; } = new Vector3( -160f, -32f, 96 );

	/// <summary>
	/// Is the camera on the right shoulder or left
	/// </summary>
	public bool IsRightShoulder { get; set; } = true;

	/// <summary>
	/// Toggles noclip on or off
	/// </summary>
	[Property]
	[Category( "Noclip" )]
	public bool ToggleNoclip { get; set; } = false;

	/// <summary>
	/// Noclip speed
	/// </summary>
	[Property]
	[Category( "Noclip" )]
	[Range( 0f, 800, 1f )]
	public float NoclipSpeed { get; set; } = 400f;

	/// <summary>
	/// Noclip sprint speed
	/// </summary>
	[Property]
	[Category( "Noclip" )]
	[Range( 400f, 1600, 1f )]
	public float NoclipSprintSpeed { get; set; } = 800f;

	/// <summary>
	/// Noclip punch, effects the up and down speed of noclip
	/// </summary>
	[Property]
	[Category( "Noclip" )]
	[Range( 0f, 250, 1f )]
	public float NoclipPunch { get; set; } = 50f;

	private readonly float DefaultAcceleration = 10f;

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineSphere( EyePosition, 10f );
	}

	protected override void OnUpdate() // Called on every frame
	{
		EyeAngles += Input.AnalogLook; // Add the analog look to the eye angles, which determinds how far the mouse moved in the last frame
		EyeAngles = EyeAngles.WithPitch( MathX.Clamp( EyeAngles.pitch, -70f, 70f ) ); // Clamp the pitch of the eye angles to prevent the player from looking too far up or down
		Transform.Rotation = Rotation.FromYaw( EyeAngles.yaw ); // Set the rotation of the player to the eye angles yaw

		if ( Camera != null )
		{

			Camera.Transform.Local = _initialCameraTransform.RotateAround( EyePosition, EyeAngles.WithYaw( 0f ) );

			if ( IsRightShoulder )
				Camera.Transform.LocalPosition = new Vector3( Camera.Transform.LocalPosition.x, -32f, Camera.Transform.LocalPosition.z ); // Move the camera to the correct Y Axis (-32) for right shoulder view
			else
				Camera.Transform.LocalPosition = new Vector3( Camera.Transform.LocalPosition.x, 32f, Camera.Transform.LocalPosition.z ); // Move the camera to the correct Y Axis (32) for left shoulder view


		}
	}

	protected override void OnFixedUpdate()
	{

		// If the controller is not set, exit
		if ( Controller == null ) return;

		// Handle input for toggling noclip and changing camera view
		HandleInput();

		// Update movement based on noclip or default movement
		UpdateMovement();

		// Update animator parameters
		UpdateAnimator();
	}

	private void HandleInput()
	{
		// Toggle camera view
		if ( Input.Pressed( "View" ) )
		{
			IsRightShoulder = !IsRightShoulder;
		}

		// Toggle noclip mode
		if ( Input.Pressed( "Walk" ) )
		{
			ToggleNoclip = !ToggleNoclip;
		}
	}

	private void UpdateMovement()
	{
		// If noclip is active, use collision rules and handle noclip movement
		if ( ToggleNoclip )
		{
			Controller.UseCollisionRules = true;
			HandleNoclip();
		}
		// If noclip is not active, handle default movement and player grounding
		else
		{
			Controller.UseCollisionRules = false;
			HandleDefaultMovement();
			HandlePlayerOnGround();
		}
	}

	private void UpdateAnimator()
	{
		// If Animator is not null, update its grounded state and velocity
		if ( Animator != null )
		{
			Animator.IsGrounded = Controller.IsOnGround;
			Animator.WithVelocity( Controller.Velocity );
		}
	}



	private void HandleNoclip()
	{
		Controller.Acceleration = DefaultAcceleration;
		Controller.ApplyFriction( 5f );

		if ( Input.Down( "Jump" ) )
		{
			Controller.Acceleration = DefaultAcceleration / 2; // Set the acceleration of the controller to 5 if the player is jumping
			Controller.Punch( Vector3.Up * NoclipPunch ); // Jump if the player is on the ground
		}

		if ( Input.Down( "Duck" ) )
		{
			Controller.Acceleration = DefaultAcceleration / 2; // Set the acceleration of the controller to 5 if the player is crouching
			Controller.Punch( Vector3.Down * NoclipPunch ); // Jump if the player is on the ground
		}

		var wishSpeed = Input.Down( "Run" ) ? NoclipSprintSpeed : NoclipSpeed; // Get the wish speed based on the player input, if crouch crouch speed else if run sprint else walk

		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * Transform.Rotation; // Get the wish velocity based on the player input AnalogMove works with WASD and controllers (Can modify inb project settings for keybinds) - If movements too fast too slow * Time.Delta CharacterController handles this for us

		Controller.Accelerate( wishVelocity ); // Accelerate CharacterController by the wish velocity
		Controller.Move(); // Move the controller
	}

	// Handles default movement of the player
	private void HandleDefaultMovement()
	{
		float wishSpeed; // The speed the player wishes to move at

		if ( Controller.IsOnGround )
			wishSpeed = Input.Down( "Duck" ) ? CrouchSpeed : (Input.Down( "Run" ) ? RunSpeed : WalkSpeed); // Get the wish speed based on the player input, if crouch crouch speed else if run sprint else walk
		else
			wishSpeed = WalkSpeed;

		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * Transform.Rotation; // Get the wish velocity based on the player input AnalogMove works with WASD and controllers (Can modify inb project settings for keybinds) - If movements too fast too slow * Time.Delta CharacterController handles this for us

		Controller.Accelerate( wishVelocity ); // Accelerate CharacterController by the wish velocity
		Controller.Move(); // Move the controller

	}

	// Handles the player when they are grounded
	private void HandlePlayerOnGround()
	{
		if ( Controller.IsOnGround )
		{
			Controller.Acceleration = DefaultAcceleration; // Set the acceleration of the controller to 10 if the player is on the ground
			Controller.ApplyFriction( 5f ); // Apply friction if the player is on the ground

			if ( Input.Pressed( "Jump" ) )
			{
				Controller.Acceleration = DefaultAcceleration / 2; // Set the acceleration of the controller to 5 if the player is jumping
				Controller.Punch( Vector3.Up * JumpHeight ); // Jump if the player is on the ground

				Animator?.TriggerJump(); // Play the jump animation if the player is on the ground
			}

			if ( Input.Pressed( "Duck" ) )
			{
				Controller.Acceleration = DefaultAcceleration / 2; // Set the acceleration of the controller to 5 if the player is crouching
				if ( Animator != null )
					Animator.DuckLevel = 1f; // Play the crouch animation if the player is crouching
			}
			else if ( Input.Released( "Duck" ) )
			{
				Controller.Acceleration = DefaultAcceleration; // Set the acceleration of the controller to 10 if the player is not crouching
				if ( Animator != null )
					Animator.DuckLevel = 0f; // Stop the crouch animation if the player is not crouching

			}
		}
		else
			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta; // Apply gravity if the player is not on the ground

	}

	protected override void OnStart() // Called when the component is first enabled
	{
		if ( Camera != null )
		{

			var createCameraPosition = Camera.Transform.Local;

			createCameraPosition.Position = InitialCameraPosition; // Set the camera position to 64 units behind the player

			_initialCameraTransform = createCameraPosition; // Set the initial camera transform to the transform of the player

			EyePosition = new Vector3( 0, 0, Controller.Height ); // Set eye position to the height of the controller

		}



		if ( Components.TryGet<SkinnedModelRenderer>( out var model ) )
		{ // Tries to get the SkinnedModelRenderer component from the player
			var clothing = ClothingContainer.CreateFromLocalUser(); // If it succeeds, create a new clothing container from the local user - This will not work in multiplayer
			clothing.Apply( model );
		}


	}

	protected override void OnEnabled() // Called when the component is enabled and after on start
	{
		base.OnEnabled();
	}

	protected override void OnDisabled() // Called when the component is disabled
	{
		base.OnDisabled();
	}

	protected override void OnDestroy() // Called when the component is removed or deleted
	{
		base.OnDestroy();
	}

	// And many more methods
}
