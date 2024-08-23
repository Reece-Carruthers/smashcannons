public sealed class SelfDestructComponent : Component
{
	[Property] public float Seconds { get; set; }

	TimeUntil timeUntilDie;

	protected override void OnEnabled()
	{
		timeUntilDie = Seconds;
	}

	protected override void OnUpdate()
	{
		if ( GameObject.IsProxy )
			return;

		if ( timeUntilDie <= 0.0f )
		{
			PlaySound();
			GameObject.Destroy();
		}
	}

	[Broadcast]
	private void PlaySound()
	{
		Sound.Play( "debris", Transform.Position );
	}
}
