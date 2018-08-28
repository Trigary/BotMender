using Blocks.Live;
using Structures;
using UnityEngine;

namespace Systems.Active {
	/// <summary>
	/// Sets the bot's velocity to zero upon activation.
	/// </summary>
	public class FullStopSystem : ActiveSystem {
		public FullStopSystem(byte id, CompleteStructure structure, RealLiveBlock block) : base(id, structure, block) { }



		public override void Activate() {
			Structure.Body.velocity = Vector3.zero;
			Structure.Body.angularVelocity = Vector3.zero;
		}
	}
}
