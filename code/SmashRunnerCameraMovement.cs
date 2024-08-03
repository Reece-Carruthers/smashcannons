using System;

public sealed class SmashRunnerCameraMovement : Component
{
	[Category( "Objects" )] [Property] public SmashRunnerMovement Player { get; set; }
	[Category( "Objects" )] [Property] public GameObject Body { get; set; }
	[Category( "Objects" )] [Property] public GameObject Head { get; set; }

	[Category( "Camera Settings" )]
	[Property]
	private float Distance { get; set; } = 0f;

	[Category( "Camera Settings" )]
	[Property]
	private float CameraZoomSpeed { get; set; } = 10f;

	[Category( "Camera Settings" )]
	[Property]
	private float MaxCameraZoom { get; set; } = 400f;

	[Category( "Camera Settings" )]
	[Property]
	private float CameraLerpSpeed { get; set; } = 5f;

	private bool IsFirstPerson => Distance <= 15f; // Prevents clipping inside of the model when zooming in, this might have to change based on FOV?
	private Vector3 CurrentOffset = Vector3.Zero;
	private CameraComponent Camera;
	private ModelRenderer BodyRenderer;
	private float targetCameraDistance;

	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
		BodyRenderer = Body.Components.Get<ModelRenderer>();
		targetCameraDistance = Distance;
	}

	protected override void OnUpdate()
	{
		var eyeAngles = Head.Transform.Rotation.Angles();
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
		Head.Transform.Rotation = eyeAngles.ToRotation();

		var targetOffset = Vector3.Zero;
		if ( Player.IsCrouching && Player.characterController.IsOnGround ) targetOffset += Vector3.Down * 32f;
		CurrentOffset = Vector3.Lerp( CurrentOffset, targetOffset, Time.Delta * 10f );

		HandleCameraZoom();

		if ( Camera is not null )
		{
			var camPos = Head.Transform.Position + CurrentOffset;
			if ( !IsFirstPerson )
			{
				var camForward = eyeAngles.ToRotation().Forward;
				var camTrace = Scene.Trace.Ray( camPos, camPos - (camForward * Distance) )
					.WithoutTags( "player", "trigger" )
					.Run();
				if ( camTrace.Hit )
				{
					camPos = camTrace.HitPosition + camTrace.Normal;
				}
				else
				{
					camPos = camTrace.EndPosition;
				}

				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else
			{
				// Hide the body if we're in first person
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}

			// Set the position of the camera to our calculated position
			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();
		}
	}

	void HandleCameraZoom()
	{
		if ( Camera is not null )
		{
			var cameraZoom = Input.MouseWheel.y * CameraZoomSpeed;
			targetCameraDistance -= cameraZoom;

			targetCameraDistance = Math.Clamp( targetCameraDistance, 0f, MaxCameraZoom );
			Distance = MathX.Lerp( Distance, targetCameraDistance, Time.Delta * CameraLerpSpeed );

			const float snapThreshold = 1f;
			if ( Math.Abs( Distance - targetCameraDistance ) < snapThreshold )
			{
				Distance = targetCameraDistance;
			}
		}
	}
}