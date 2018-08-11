using System.Collections.Generic;
using System.Linq;
using Blocks;
using Blocks.Shared;
using UnityEngine.Assertions;

namespace Structures {
	/// <summary>
	/// Utility methods for both the EditableStructure and the CompleteStructure classes.
	/// </summary>
	public static class StructureUtilities {
		/// <summary>
		/// Removes the blocks from the dictionary which are connected to the specified block (directly or not).
		/// </summary>
		public static void RemoveConnected<T>(T block, IDictionary<BlockPosition, T> blocks) where T : IBlock {
			RemoveConnectedBlocks(block, -1, blocks);
			foreach (KeyValuePair<BlockPosition, T> pair in blocks.Where(pair => pair.Value is IMultiBlockPart).ToList()) {
				blocks.Remove(pair.Key);
			}
		}

		private static void RemoveConnectedBlocks<T>(T block, int ignoreBit, IDictionary<BlockPosition, T> blocks) where T : IBlock {
			Assert.IsTrue(blocks.Remove(block.Position), "The block is no longer in the dictionary.");
			for (int bit = 0; bit < 6; bit++) {
				if (bit == ignoreBit) {
					continue;
				}

				BlockSides side = block.ConnectSides & (BlockSides)(1 << bit);
				if (side == BlockSides.None
					|| !block.Position.GetOffseted(side, out BlockPosition offseted)
					|| !blocks.TryGetValue(offseted, out T other)) {
					continue;
				}

				int inverseBit = bit % 2 == 0 ? bit + 1 : bit - 1;
				if ((other.ConnectSides & (BlockSides)(1 << inverseBit)) != BlockSides.None) {
					RemoveConnectedBlocks(other, inverseBit, blocks);
				}
			}
		}
	}
}
