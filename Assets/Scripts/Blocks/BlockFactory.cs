using System;
using System.Collections.Generic;
using Blocks.Info;
using Blocks.Live;
using Blocks.Placed;
using Blocks.Shared;
using Boo.Lang;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Blocks {
	/// <summary>
	/// Creates new block GameObject instances.
	/// </summary>
	public static class BlockFactory {
		private static readonly BlockType[] BlockTypes = (BlockType[])Enum.GetValues(typeof(BlockType));
		private static readonly BlockInfo[] Blocks = new BlockInfo[BlockTypes.Length];

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
			AddSingle(BlockType.ArmorInner1, 875, 875, BlockSides.All);

			AddMulti(BlockType.ArmorLong1, 2000, 2000)
				.Add(0, 0, 0, BlockSides.All)
				.Add(0, 1, 0, BlockSides.All)
				.Finished();

			AddSingle(BlockType.LaserWeapon1, 2000, 2000, BlockSides.Bottom | BlockSides.Back);

			AddSingle(BlockType.ThrusterSmall, 500, 500, BlockSides.Bottom);
			AddMulti(BlockType.UnrealAccelerator, 2000, 2000)
				.Add(0, 0, 0, BlockSides.Y)
				.Add(0, 1, 0, BlockSides.Y)
				.Finished();

			AddSingle(BlockType.FullStopSystem, 500, 500, BlockSides.Bottom);
		}



		/// <summary>
		/// Gets the count of the types. If the index specified in #GetType is greater than or equal to this an error happens.
		/// </summary>
		public static int TypeCount => BlockTypes.Length;

		/// <summary>
		/// Gets the BlockType associated with the specified index. This method is useful for serializations.
		/// See #TypeCount for the safeness of this method.
		/// </summary>
		public static BlockType GetType(ushort index) {
			return BlockTypes[index];
		}

		/// <summary>
		/// Gets the BlockInfo associated with the specified block type.
		/// </summary>
		public static BlockInfo GetInfo(BlockType type) {
			return Blocks[(ushort)type];
		}



		public static PlacedSingleBlock MakeSinglePlaced(Transform parent, SingleBlockInfo info, byte rotation, BlockPosition position) {
			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			PlacedSingleBlock component = block.AddComponent<PlacedSingleBlock>();
			component.Initialize(info, position, rotation);
			return component;
		}

		/// <summary>
		/// Creates a multi-block. Returns null if it fails.
		/// </summary>
		[CanBeNull]
		public static PlacedMultiBlockParent MakeMultiPlaced(Transform parent, MultiBlockInfo info, byte rotation, BlockPosition position,
															out PlacedMultiBlockPart[] parts) {
			KeyValuePair<BlockPosition, BlockSides>[] partPositions;
			if (!info.GetRotatedPositions(position, rotation, out partPositions)) {
				parts = null;
				return null;
			}

			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			PlacedMultiBlockParent component = block.AddComponent<PlacedMultiBlockParent>();

			IMultiBlockPart[] tempParts;
			// ReSharper disable once CoVariantArrayConversion
			InitializeMulti(component, info, rotation, position, partPositions, count => new PlacedMultiBlockPart[count],
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

		/// <summary>
		/// Creates a multi-block. Returns null if it fails.
		/// </summary>
		[CanBeNull]
		public static LiveMultiBlockParent MakeMultiLive(Transform parent, MultiBlockInfo info, byte rotation, BlockPosition position,
														out LiveMultiBlockPart[] parts) {
			KeyValuePair<BlockPosition, BlockSides>[] partPositions;
			if (!info.GetRotatedPositions(position, rotation, out partPositions)) {
				parts = null;
				return null;
			}

			GameObject block = InstantiatePrefab(parent, info, rotation, position);
			LiveMultiBlockParent component = block.AddComponent<LiveMultiBlockParent>();

			IMultiBlockPart[] tempParts;
			// ReSharper disable once CoVariantArrayConversion
			InitializeMulti(component, info, rotation, position, partPositions, count => new LiveMultiBlockPart[count],
				pair => new LiveMultiBlockPart(pair.Value, pair.Key), out tempParts);
			parts = (LiveMultiBlockPart[])tempParts;
			return component;
		}



		private static void AddSingle(BlockType type, uint health, uint mass, BlockSides connectSides) {
			Blocks[(ushort)type] = new SingleBlockInfo(type, health, mass, Resources.Load("Blocks/" + type) as GameObject,
				connectSides);
		}

		private static MultiBlockInfo AddMulti(BlockType type, uint health, uint mass) {
			MultiBlockInfo info = new MultiBlockInfo(type, health, mass, Resources.Load("Blocks/" + type) as GameObject);
			Blocks[(ushort)type] = info;
			return info;
		}



		private static GameObject InstantiatePrefab(Transform parent, BlockInfo info, byte rotation, BlockPosition position) {
			return Object.Instantiate(info.Prefab, position.ToVector(), Rotation.GetQuaternion(rotation), parent);
		}

		private static void InitializeMulti(IMultiBlockParent parent, MultiBlockInfo info, byte rotation, BlockPosition position,
											KeyValuePair<BlockPosition, BlockSides>[] partPositions,
											Function<int, IMultiBlockPart[]> partsArrayConstructor,
											Function<KeyValuePair<BlockPosition, BlockSides>, IMultiBlockPart> partConstructor,
											out IMultiBlockPart[] parts) {
			BlockSides parentSides = BlockSides.None;
			parts = partsArrayConstructor.Invoke(partPositions.Length - 1);
			int partsIndex = 0;
			foreach (KeyValuePair<BlockPosition, BlockSides> pair in partPositions) {
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
