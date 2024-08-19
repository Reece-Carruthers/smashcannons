using Sandbox;
using System;
using System.Linq;
using Sandbox.Diagnostics;

public sealed class RagdollController : Component
{
	[HostSync] public bool IsRagdolled { get; private set; }
	[Property] public ModelPhysics Physics { get; set; }

	[Broadcast( NetPermission.HostOnly )]
	public void Ragdoll( Vector3 position, Vector3 force )
	{
		Physics.Enabled = true;
		IsRagdolled = true;
		Tags.Add( "corpse" );
		
	}

	[Broadcast( NetPermission.HostOnly )]
	public void Unragdoll()
	{
		Physics.Enabled = false;
		IsRagdolled = false;
		Tags.Remove( "corpse" );
	}
}
