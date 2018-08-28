using System.Collections.Generic;
using Systems.Propulsion;
using Systems.Weapon;
using Blocks;
using UnityEngine;

namespace Systems {
	/// <summary>
	/// A class which acts as the container of the constants registered inside it.
	/// The constants can be accessed through the public variables of the class.
	/// </summary>
	public static class SystemConstantsContainer {
		public static readonly IDictionary<BlockType, WeaponSystem.WeaponConstants> WeaponConstants = new Dictionary<BlockType, WeaponSystem.WeaponConstants>();
		public static readonly IDictionary<BlockType, ThrusterSystem.ThrusterConstants> ThrusterConstants = new Dictionary<BlockType, ThrusterSystem.ThrusterConstants>();

		static SystemConstantsContainer() {
			WeaponConstants[BlockType.LaserWeapon1] = new WeaponSystem.WeaponConstants(WeaponSystem.Type.Laser, new Vector3(0, 0, 0.5f), 120, -60, 30, 300, 5, 1, 0.25f, 8);

			ThrusterConstants[BlockType.ThrusterSmall] = new ThrusterSystem.ThrusterConstants(Vector3.zero, 1, BlockSides.Front);
		}
	}
}
