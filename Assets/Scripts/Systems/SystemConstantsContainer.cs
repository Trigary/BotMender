using System.Collections.Generic;
using Assets.Scripts.Blocks;
using Assets.Scripts.Systems.Propulsion;
using UnityEngine;

namespace Assets.Scripts.Systems {
	public static class SystemConstantsContainer {
		public static readonly IDictionary<BlockType, WeaponSystem.WeaponConstants> WeaponConstants = new Dictionary<BlockType, WeaponSystem.WeaponConstants>();
		public static readonly IDictionary<BlockType, ThrusterSystem.ThrusterConstants> ThrusterConstants = new Dictionary<BlockType, ThrusterSystem.ThrusterConstants>();

		static SystemConstantsContainer() {
			WeaponConstants[BlockType.LaserWeapon1] = new WeaponSystem.WeaponConstants(new Vector3(0, 0, 0.5f), 120, -60, 30, 300, 5, 1, 0.25f, 8);

			ThrusterConstants[BlockType.ThrusterSmall] = new ThrusterSystem.ThrusterConstants(Vector3.zero, 1, BlockSides.Front);
		}
	}
}
