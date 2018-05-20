using System;
using System.Collections.Generic;
using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Blocks.Live;
using Assets.Scripts.Blocks.Placed;
using Assets.Scripts.Blocks.Shared;
using Boo.Lang;
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
															out PlacedMultiBlockPart[] parts) {
			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			PlacedMultiBlockParent component = block.AddComponent<PlacedMultiBlockParent>();

			IMultiBlockPart[] tempParts;
			// ReSharper disable once CoVariantArrayConversion
			InitializeMulti(component, info, rotation, position, count => new PlacedMultiBlockPart[count],
				pair => new PlacedMultiBlockPart(pair.Value, pair.Key), out tempParts);
			parts = (PlacedMultiBlockPart[])tempParts;
			return component;
		}



		public static LiveSingleBlock MakeSingleLive(Transform parent, SingleBlockInfo info, byte rotation, BlockPosition position) {
			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			LiveSingleBlock component = block.AddComponent<LiveSingleBlock>();
			component.Initialize(info, position, rotation);
			return component;
		}

		public static LiveMultiBlockParent MakeMultiLive(Transform parent, MultiBlockInfo info, byte rotation, BlockPosition position,
														out LiveMultiBlockPart[] parts) {
			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			LiveMultiBlockParent component = block.AddComponent<LiveMultiBlockParent>();

			IMultiBlockPart[] tempParts;
			// ReSharper disable once CoVariantArrayConversion
			InitializeMulti(component, info, rotation, position, count => new LiveMultiBlockPart[count],
				pair => new LiveMultiBlockPart(pair.Value, pair.Key), out tempParts);
			parts = (LiveMultiBlockPart[])tempParts;
			return component;
		}



		private static void AddSingle(BlockType type, uint health, uint mass, BlockSides connectSides) {
			Blocks.Add(type, new SingleBlockInfo(type, health, mass, Resources.Load("Blocks/" + type) as GameObject,
				connectSides));
		}

		private static MultiBlockInfo AddMulti(BlockType type, uint health, uint mass) {
			MultiBlockInfo info = new MultiBlockInfo(type, health, mass, Resources.Load("Blocks/" + type) as GameObject);
			Blocks.Add(type, info);
			return info;
		}



		private static GameObject InstantiatePrefab(Transform parent, BlockInfo info, byte rotation, BlockPosition position) {
			return Object.Instantiate(info.Prefab, position.ToVector(), Rotation.GetQuaternion(rotation), parent);
		}

		private static void InitializeMulti(IMultiBlockParent parent, MultiBlockInfo info, byte rotation, BlockPosition position,
											Function<int, IMultiBlockPart[]> partsArrayConstructor,
											Function<KeyValuePair<BlockPosition, BlockSides>, IMultiBlockPart> partConstructor,
											out IMultiBlockPart[] parts) {
			KeyValuePair<BlockPosition, BlockSides>[] positions;
			info.GetRotatedPositions(position, rotation, out positions);

			BlockSides parentSides = BlockSides.None;
			parts = partsArrayConstructor.Invoke(positions.Length - 1);
			int partsIndex = 0;
			foreach (KeyValuePair<BlockPosition, BlockSides> pair in positions) {
				if (pair.Key.Equals(position)) {
					parentSides = pair.Value;
				} else {
					parts[partsIndex++] = partConstructor.Invoke(pair);
				}
			}

			foreach (IMultiBlockPart part in parts) {
				part.Initialize(parent);
			}
			parent.Initialize(parentSides, position, info, rotation, parts);
		}
	}
}
