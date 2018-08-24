using Blocks.Live;
using Structures;
using UnityEngine;

namespace Systems.Propulsion {
	/// <summary>
	/// Changes the bot's velocity directly: doesn't apply any forces.
	/// </summary>
	public class UnrealAcceleratorSystem : PropulsionSystem {
		public UnrealAcceleratorSystem(byte id, CompleteStructure structure, RealLiveBlock block) : base(id, structure, block) { }



		public override void MoveRotate(Rigidbody bot, Vector3 direction, float timestepMultiplier) {
			bot.AddForce(bot.transform.rotation * new Vector3(0, direction.y, direction.z) * timestepMultiplier,
				ForceMode.VelocityChange);

			Vector3 angularVelocity = bot.angularVelocity;
			angularVelocity.y = Mathf.Clamp(angularVelocity.y + direction.x * 0.35f * timestepMultiplier, -1.5f, 1.5f);
			bot.angularVelocity = angularVelocity;
		}
	}
}
