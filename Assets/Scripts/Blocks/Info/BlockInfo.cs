using UnityEngine;

namespace Assets.Scripts.Blocks.Info {
	public abstract class BlockInfo {
		public readonly BlockType Type;
		public readonly uint Health;
		public readonly uint Mass;
		public readonly GameObject Prefab;

		protected BlockInfo(BlockType type, uint health, uint mass, GameObject prefab) {
			Type = type;
			Health = health;
			Mass = mass;
			Prefab = prefab;
		}
	}
}
