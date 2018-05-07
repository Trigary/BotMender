using UnityEngine;

namespace Assets.Scripts.Blocks.Info {
	public class SingleBlockInfo : BlockInfo {
		public readonly BlockSides ConnectSides;

		public SingleBlockInfo(BlockType type, uint health, uint mass, GameObject prefab, BlockSides connectSides) :
			base(type, health, mass, prefab) {
			ConnectSides = connectSides;
		}
	}
}
