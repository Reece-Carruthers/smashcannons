using System;

public sealed class SmashRunnerCameraMovement : Component, Component.ICollisionListener
{
	[Category( "Camera Settings" )]
	[Property]
	private float Distance { get; set; } = 300f;

	[Category( "Camera Settings" )]
	[Property]
	private float CameraZoomSpeed { get; set; } = 10f;

	[Category( "Camera Settings" )]
	[Property]
	private float MaxCameraZoom { get; set; } = 400f;

	[Category( "Camera Settings" )]
	[Property]
	private float CameraLerpSpeed { get; set; } = 5f;

	private SmashRunnerMovement Player { get; set; }
	private GameObject Body { get; set; }
	private GameObject Head { get; set; }

	private bool IsFirstPerson =>
		Distance <= 20f;

	private Vector3 CurrentOffset = Vector3.Zero;
	private CameraComponent Camera;
	private float targetCameraDistance = 300f;
	private SkinnedModelRenderer modelRenderer;
	private bool lastIsFirstPerson;

	protected override void OnUpdate()
	{
		if ( Player?.LifeState == LifeState.Dead ) return; // Prevent camera movement on death

		if ( Network.IsProxy ) return;

		if ( Player is null )
		{
			Player = SmashRunnerMovement.Local;
			if ( Player is not null )
			{
				Body = Player.Body;
				Head = Player.Head;
				modelRenderer = Body.Components.Get<SkinnedModelRenderer>();
				Camera = Components.Get<CameraComponent>();
				Camera.FieldOfView = 90f;
				lastIsFirstPerson = IsFirstPerson;
			}
		}

		if ( Player is null ) return;

		var eyeAngles = Head.WorldRotation.Angles();
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -80f, 80f );
		Head.WorldRotation = eyeAngles.ToRotation();

		var targetOffset = Vector3.Zero;
		CurrentOffset = Vector3.Lerp( CurrentOffset, targetOffset, Time.Delta * 10f );

		if ( Camera is null ) return;

		HandleCameraZoom();

		var camPos = Head.WorldPosition + CurrentOffset;
		if ( !IsFirstPerson )
		{
			var camForward = eyeAngles.ToRotation().Forward;
			var camTrace = Scene.Trace.Ray( camPos, camPos - (camForward * Distance) )
				.WithoutTags( "player", "trigger", "cameraIgnore" )
				.Run();
			if ( camTrace.Hit )
			{
				camPos = camTrace.HitPosition + camTrace.Normal;
			}
			else
			{
				camPos = camTrace.EndPosition;
			}
		}

		if ( IsFirstPerson != lastIsFirstPerson )
		{
			UpdateRenderTypes();
			lastIsFirstPerson = IsFirstPerson;
		}

		Camera.WorldPosition = camPos;
		Camera.WorldRotation = eyeAngles.ToRotation();
	}

	private void HandleCameraZoom()
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

	private void
		UpdateRenderTypes()
	{
		if ( modelRenderer is null ) return;

		modelRenderer.RenderType =
			IsFirstPerson ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
		var clothingList = Body.Components.GetAll<SkinnedModelRenderer>( FindMode.EverythingInDescendants )
			.Where( x => x.Tags.Has( "clothing" ) );
		foreach ( var clothing in clothingList )
		{
			clothing.RenderType = IsFirstPerson
				? ModelRenderer.ShadowRenderType.ShadowsOnly
				: ModelRenderer.ShadowRenderType.On;
		}
	}
}
