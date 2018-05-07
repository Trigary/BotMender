using UnityEngine;

namespace Assets.Scripts.Systems.Propulsion {
	public class UnrealAcceleratorSystem : PropulsionSystem {
		public override void MoveRotate(Rigidbody bot, float x, float y, float z) {
			bot.AddForce(bot.transform.rotation * new Vector3(0, y, z), ForceMode.VelocityChange);
			bot.transform.RotateAround(bot.transform.position, bot.transform.up, x);
		}
	}
}
