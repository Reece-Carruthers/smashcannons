using System;
using Sandbox;

public class FinalState : ExtendedState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();

	public override string Name => "Last Phase";
	[Sync] private TimeUntil RoundEndTime { get; set; }

	protected override void OnEnter()
	{
		RoundEndTime = 60f;

		HideAndShowObjects();
		TeleportToRamp();
	}

	private void HideAndShowObjects()
	{
		var mainScript = SmashCannon.Instance;

		if ( mainScript is null ) return;

		if ( mainScript.Platforms != null )
		{
			mainScript.Platforms.Enabled = false;
		}

		if ( mainScript.Pillars != null )
		{
			mainScript.Pillars.Enabled = false;
		}

		if ( mainScript.Ramp != null )
		{
			mainScript.Ramp.Enabled = true;
		}
		
		if ( mainScript.KillButton != null )
		{
			mainScript.KillButton.Enabled = true;
		}
	}

	private void TeleportToRamp()
	{
		FetchAlivePlayers();

		var rampSpawns = Scene.Components.GetAll<PlayerSpawn>()
			.Where( x => x.Tags.Has( "ramp_spawn" ) ).ToList();

		foreach ( var player in ActiveRunnerPlayers )
		{
			player.Teleport( RandomRampSpawn( rampSpawns ) );
		}
	}

	private Vector3 RandomRampSpawn( List<PlayerSpawn> rampSpawns )
	{
		var random = new Random();

		var randomIndex = random.Next( rampSpawns.Count );
		var selectedSpawn = rampSpawns[randomIndex];

		return selectedSpawn.Transform.Position;
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			FetchAlivePlayers();

			if ( Connection.All.Count > 1 && (ActiveRunnerPlayers.Count <= 0 || ActiveCannonPlayers.Count <= 0) )
			{
				StateSystem.Set<EndState>();
			}

			if ( RoundEndTime )
			{
				StateSystem.Set<EndState>();
			}

			if ( Connection.All.Count <= 1 ) // Restart game on only having one connection
			{
				Game.ActiveScene.LoadFromFile( "scenes/smashtowermap.scene" );
			}
		}
	}
}
