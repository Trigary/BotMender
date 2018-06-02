using System.Collections.Generic;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Live;
using Assets.Scripts.Systems.Active;
using Assets.Scripts.Systems.Propulsion;
using Assets.Scripts.Systems.Weapon;
using Boo.Lang;

namespace Assets.Scripts.Systems {
	/// <summary>
	/// Creates new system instances.
	/// </summary>
	public static class SystemFactory {
		private static readonly IDictionary<BlockType, Function<RealLiveBlock, BotSystem>> Constructors =
			new Dictionary<BlockType, Function<RealLiveBlock, BotSystem>>();

		static SystemFactory() {
			Add(BlockType.LaserWeapon1, block => new LaserSystem(block, SystemConstantsContainer.WeaponConstants[block.Info.Type]));

			Add(BlockType.ThrusterSmall, block => new ThrusterSystem(block, SystemConstantsContainer.ThrusterConstants[block.Info.Type]));
			Add(BlockType.UnrealAccelerator, block => new UnrealAcceleratorSystem(block));

			Add(BlockType.FullStopSystem, block => new FullStopSystem(block));
		}



		/// <summary>
		/// Create a new system instance from the block if the block comes with a system.
		/// </summary>
		public static bool Create(RealLiveBlock block, out BotSystem system) {
			Function<RealLiveBlock, BotSystem> function;
			if (!Constructors.TryGetValue(block.Info.Type, out function)) {
				system = null;
				return false;
			}

			system = function.Invoke(block);
			return true;
		}

		/// <summary>
		/// Returns the weapon type of the specified block type. If it is not a weapon, #None is returned.
		/// </summary>
		public static WeaponSystem.Type GetWeaponType(BlockType block) {
			switch (block) {
				case BlockType.LaserWeapon1:
					return WeaponSystem.Type.Laser;
				default:
					return WeaponSystem.Type.None;
			}
		}

		/// <summary>
		/// Returns whether the specified block type installs an active system.
		/// </summary>
		public static bool IsActiveSystem(BlockType block) {
			switch (block) {
				case BlockType.FullStopSystem:
					return true;
				default:
					return false;
			}
		}



		private static void Add(BlockType type, Function<RealLiveBlock, BotSystem> function) {
			Constructors.Add(type, function);
		}
	}
}
