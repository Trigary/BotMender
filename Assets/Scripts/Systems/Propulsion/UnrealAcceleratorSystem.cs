using Blocks.Live;
using UnityEngine;

namespace Systems.Propulsion {
	/// <summary>
	/// Changes the bot's velocity directly: doesn't apply any forces.
	/// </summary>
	public class UnrealAcceleratorSystem : PropulsionSystem {
		public UnrealAcceleratorSystem(RealLiveBlock block) : base(block) { }



		public override void MoveRotate(Rigidbody bot, Vector3 direction, float timestepMultiplier) {
			bot.AddForce(bot.transform.rotation * new Vector3(0, direction.y, direction.z) * timestepMultiplier,
				ForceMode.VelocityChange);
			bot.transform.RotateAround(bot.transform.position, bot.transform.up, direction.x * timestepMultiplier);
		}
	}
}
