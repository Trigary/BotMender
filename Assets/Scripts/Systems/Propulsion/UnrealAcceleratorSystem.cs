using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems.Propulsion {
	/// <summary>
	/// Changes the bot's velocity directly: doesn't apply any forces.
	/// </summary>
	public class UnrealAcceleratorSystem : PropulsionSystem {
		public UnrealAcceleratorSystem(RealLiveBlock block) : base(block) { }



		public override void MoveRotate(Rigidbody bot, float x, float y, float z) {
			bot.AddForce(bot.transform.rotation * new Vector3(0, y, z), ForceMode.VelocityChange);
			bot.transform.RotateAround(bot.transform.position, bot.transform.up, x);
		}
	}
}
