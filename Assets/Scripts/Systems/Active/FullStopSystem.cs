using Blocks.Live;
using Structures;
using UnityEngine;

namespace Systems.Active {
	/// <summary>
	/// Sets the structure's velocity to zero upon activation.
	/// </summary>
	public class FullStopSystem : ActiveSystem {
		public FullStopSystem(CompleteStructure structure, RealLiveBlock block) : base(structure, block) { }



		public override void Activate() {
			Structure.Body.velocity = Vector3.zero;
			Structure.Body.angularVelocity = Vector3.zero;
		}
	}
}
