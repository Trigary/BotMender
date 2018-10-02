using Blocks.Info;
using Blocks.Shared;

namespace Blocks.Placed {
	/// <summary>
	/// A real placed block which is a part of a multi block and is the parent of the multi block parts.
	/// </summary>
	public class PlacedMultiBlockParent : RealPlacedBlock, IMultiBlockParent {
		public PlacedMultiBlockPart[] Parts { get; private set; }

		public void Initialize(BlockSides connectSides, BlockPosition position, MultiBlockInfo info, byte rotation,
								IMultiBlockPart[] parts) {
			ConnectSides = connectSides;
			Position = position;
			Type = info.Type;
			Rotation = rotation;
			Parts = (PlacedMultiBlockPart[])parts;
		}
	}
}
