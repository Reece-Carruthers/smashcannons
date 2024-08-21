using Sandbox;

public abstract class ExtendedState : BaseState
{
	[Sync] public List<SmashRunnerMovement> ActiveRunnerPlayers { get; set; } = new List<SmashRunnerMovement>();
	[Sync] public List<SmashRunnerMovement> ActiveCannonPlayers { get; set; } = new List<SmashRunnerMovement>();
	
	protected void FetchAlivePlayers()
	{
		ActiveRunnerPlayers = SmashCannon.Players
			.Where( player => player.LifeState == LifeState.Alive && player.TeamCategory is RunnerTeam ).ToList();
		ActiveCannonPlayers = SmashCannon.Players
			.Where( player => player.LifeState == LifeState.Alive && player.TeamCategory is SmashTeam ).ToList();
	}
	
}
