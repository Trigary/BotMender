using System;
using System.Collections.Generic;
using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Blocks.Live;
using Assets.Scripts.Blocks.Placed;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Scripts.Blocks {
	/// <summary>
	/// Creates new block GameObject instances.
	/// </summary>
	public static class BlockFactory {
		private static readonly BlockType[] BlockTypes = (BlockType[])Enum.GetValues(typeof(BlockType));
		private static readonly IDictionary<BlockType, BlockInfo> Blocks = new Dictionary<BlockType, BlockInfo>();

		static BlockFactory() {
			AddMulti(BlockType.Mainframe, 5000, 2500)
				.Add(0, 0, 0, BlockSides.All)
				.Add(0, 0, 1, BlockSides.All)
				.Add(0, 0, 2, BlockSides.All)
				.Add(0, 1, 0, BlockSides.All)
				.Add(0, 1, 1, BlockSides.All)
				.Add(0, 1, 2, BlockSides.All)
				.Finished();

			AddSingle(BlockType.ArmorCube1, 1000, 1000, BlockSides.All);
			AddSingle(BlockType.ArmorSlope1, 500, 500, BlockSides.X | BlockSides.Bottom | BlockSides.Back);
			AddSingle(BlockType.ArmorCorner1, 125, 125, BlockSides.Left | BlockSides.Bottom | BlockSides.Back);

			AddMulti(BlockType.ArmorLong1, 2000, 2000)
				.Add(0, 0, 0, BlockSides.All)
				.Add(0, 1, 0, BlockSides.All)
				.Finished();
		}



		public static int TypeCount { get { return BlockTypes.Length; } }

		public static BlockType GetType(int index) {
			return BlockTypes[index];
		}

		public static BlockInfo GetInfo(BlockType type) {
			return Blocks[type];
		}



		public static PlacedSingleBlock MakeSinglePlaced(Transform parent, SingleBlockInfo info, byte rotation, BlockPosition position) {
			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			PlacedSingleBlock component = block.AddComponent<PlacedSingleBlock>();
			component.Initialize(info, position, rotation);
			return component;
		}

		public static PlacedMultiBlockParent MakeMultiPlaced(Transform parent, MultiBlockInfo info, byte rotation, BlockPosition position,
															BlockSides connectSides, PlacedMultiBlockPart[] parts) {
			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			PlacedMultiBlockParent component = block.AddComponent<PlacedMultiBlockParent>();
			component.Initialize(connectSides, position, info, rotation, parts);
			return component;
		}



		public static LiveSingleBlock MakeSingleLive(Transform parent, SingleBlockInfo info, byte rotation, BlockPosition position) {
			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			LiveSingleBlock component = block.AddComponent<LiveSingleBlock>();
			component.Initialize(info, position, rotation);
			return component;
		}

		public static LiveMultiBlockParent MakeMultiLive(Transform parent, MultiBlockInfo info, byte rotation, BlockPosition position,
														BlockSides connectSides, LiveMultiBlockPart[] parts) {
			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			LiveMultiBlockParent component = block.AddComponent<LiveMultiBlockParent>();
			component.Initialize(connectSides, position, info, rotation, parts);
			return component;
		}



		private static GameObject InstantiatePrefab(Transform parent, BlockInfo info, byte rotation, BlockPosition position) {
			return Object.Instantiate(info.Prefab, position.ToVector(), Rotation.GetQuaternion(rotation), parent);
		}

		private static void AddSingle(BlockType type, uint health, uint mass, BlockSides connectSides) {
			Blocks.Add(type, new SingleBlockInfo(type, health, mass, Resources.Load("Blocks/" + type) as GameObject, connectSides));
		}

		private static MultiBlockInfo AddMulti(BlockType type, uint health, uint mass) {
			MultiBlockInfo info = new MultiBlockInfo(type, health, mass, Resources.Load("Blocks/" + type) as GameObject);
			Blocks.Add(type, info);
			return info;
		}
	}
}
