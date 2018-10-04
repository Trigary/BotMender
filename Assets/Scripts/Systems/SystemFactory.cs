using System.Collections.Generic;
using Systems.Active;
using Systems.Propulsion;
using Systems.Weapon;
using Blocks;
using Blocks.Live;
using Structures;

namespace Systems {
	/// <summary>
	/// Creates new system instances.
	/// All systems have to be specified in this class, possibly in multiple places.
	/// If the system has different parameters based on the block's exact type,
	/// the SystemConstantsContainer class should be used for storing block-specific system constants.
	/// </summary>
	public static class SystemFactory {
		private delegate BotSystem SystemConstructor(CompleteStructure structure, RealLiveBlock block);
		private static readonly IDictionary<BlockType, SystemConstructor> Constructors = new Dictionary<BlockType, SystemConstructor>();

		static SystemFactory() {
			Add(BlockType.LaserWeapon1, (structure, block) => new LaserWeapon(structure, block, SystemConstantsContainer.WeaponConstants[block.Info.Type]));

			Add(BlockType.ThrusterSmall, (structure, block) => new ThrusterSystem(structure, block, SystemConstantsContainer.ThrusterConstants[block.Info.Type]));
			Add(BlockType.UnrealAccelerator, (structure, block) => new UnrealAcceleratorSystem(structure, block));

			Add(BlockType.FullStopSystem, (structure, block) => new FullStopSystem(structure, block));
		}



		/// <summary>
		/// Create a new system instance from the block if the block comes with a system.
		/// </summary>
		public static bool Create(CompleteStructure structure, RealLiveBlock block, out BotSystem system) {
			if (!Constructors.TryGetValue(block.Info.Type, out SystemConstructor constructor)) {
				system = null;
				return false;
			}

			system = constructor(structure, block);
			return true;
		}



		/// <summary>
		/// Returns whether the specified block type installs any system.
		/// </summary>
		public static bool IsAnySystem(BlockType block) {
			return Constructors.ContainsKey(block);
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



		private static void Add(BlockType type, SystemConstructor constructor) {
			Constructors.Add(type, constructor);
		}
	}
}
