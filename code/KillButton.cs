using Sandbox;

public sealed class KillButton : Component
{
	[Sync] public SmashRunnerMovement Presser { get; set; } = null;

	protected override void OnUpdate()
	{
	}

	[Broadcast]
	public void Kill( SmashRunnerMovement presser )
	{
		Presser = presser;

		var finalState = StateSystem.Active as FinalState;

		if ( finalState is null ) return;

		var cannonPlayers = finalState.ActiveCannonPlayers;

		foreach ( var cannonPlayer in cannonPlayers )
		{
			cannonPlayer.Kill();
		}
	}
}
