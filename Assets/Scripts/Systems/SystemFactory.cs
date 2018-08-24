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
	/// </summary>
	public static class SystemFactory {
		private delegate BotSystem SystemConstructor(byte id, CompleteStructure structure, RealLiveBlock block);
		private static readonly IDictionary<BlockType, SystemConstructor> Constructors = new Dictionary<BlockType, SystemConstructor>();

		static SystemFactory() {
			Add(BlockType.LaserWeapon1, (id, structure, block) => new LaserSystem(id, structure, block, SystemConstantsContainer.WeaponConstants[block.Info.Type]));

			Add(BlockType.ThrusterSmall, (id, structure, block) => new ThrusterSystem(id, structure, block, SystemConstantsContainer.ThrusterConstants[block.Info.Type]));
			Add(BlockType.UnrealAccelerator, (id, structure, block) => new UnrealAcceleratorSystem(id, structure, block));

			Add(BlockType.FullStopSystem, (id, structure, block) => new FullStopSystem(id, structure, block));
		}



		/// <summary>
		/// Create a new system instance from the block if the block comes with a system.
		/// </summary>
		public static bool Create(byte id, CompleteStructure structure, RealLiveBlock block, out BotSystem system) {
			if (!Constructors.TryGetValue(block.Info.Type, out SystemConstructor constructor)) {
				system = null;
				return false;
			}

			system = constructor(id, structure, block);
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
