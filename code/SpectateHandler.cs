using Sandbox;

public sealed class SpectateHandler : Component
{
	private List<SmashRunnerMovement> DeadPlayers = new List<SmashRunnerMovement>();
	
	protected override void OnUpdate()
	{
		FetchTheDead();
		AssignTimer();
		RespawnAsSpectator();
	}


	private void FetchTheDead()
	{
		DeadPlayers = Scene.Components.GetAll<SmashRunnerMovement>()
			.Where( x => x.IsValid() && x.LifeState == LifeState.Dead ).ToList();
	}

	private void AssignTimer()
	{
		foreach ( var deadPlayer in DeadPlayers )
		{
			if ( deadPlayer.DeathTimerAssigned ) continue;

			deadPlayer.DeathTimer = 3f;
			deadPlayer.DeathTimerAssigned = true;
		}
	}
	
	private void RespawnAsSpectator()
	{
		if ( !Networking.IsHost ) return;

		foreach ( var deadPlayer in DeadPlayers.ToList() )
		{
			if ( !deadPlayer.DeathTimer || deadPlayer.LifeState != LifeState.Dead ) continue;

			deadPlayer.HandleSpectate();
			DeadPlayers.Remove( deadPlayer );
			deadPlayer.DeathTimerAssigned = false;
		}
	}
}
