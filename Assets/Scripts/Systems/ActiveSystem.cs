using Blocks.Live;
using UnityEngine;

namespace Systems {
	/// <summary>
	/// A system which is activated by pressing a specific button.
	/// Only one system of this type can exists in a bot.
	/// </summary>
	public abstract class ActiveSystem : BotSystem {
		protected ActiveSystem(RealLiveBlock block) : base(block) { }



		/// <summary>
		/// Activate the system.
		/// </summary>
		public abstract void Activate(Rigidbody bot);
	}
}
