using Sandbox;

public class LobbyState : BaseState
{
	public override int TimeLeft => RoundEndTime.Relative.CeilToInt();
	[Sync] public TimeUntil RoundEndTime { get; set; }
	
	private bool PlayedCountdown { get; set; }

	protected override void OnEnter()
	{
		Log.Info("in lobby state!"  );
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			if ( RoundEndTime )
			{
				StateSystem.Set<GameState>();
				return;
			}
		}

		if ( RoundEndTime <= 5f && !PlayedCountdown )
		{
			PlayedCountdown = true;
		}
		
		base.OnUpdate();
	}
}
