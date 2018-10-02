using Blocks.Info;
using Blocks.Shared;

namespace Blocks.Live {
	/// <summary>
	/// A real live block which is a part of a multi block and is the parent of the multi block parts.
	/// </summary>
	public class LiveMultiBlockParent : RealLiveBlock, IMultiBlockParent {
		public LiveMultiBlockPart[] Parts { get; private set; }

		public void Initialize(BlockSides connectSides, BlockPosition position, MultiBlockInfo info, byte rotation,
								IMultiBlockPart[] parts) {
			ConnectSides = connectSides;
			Position = position;
			Info = info;
			Rotation = rotation;
			Parts = (LiveMultiBlockPart[])parts;
			InitializeBase();
		}
	}
}
