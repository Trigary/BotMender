using Blocks.Shared;

namespace Blocks.Placed {
	/// <summary>
	/// A placed block which may be a real placed block (has a GameObject) or a part of a multiblock.
	/// </summary>
	public interface IPlacedBlock : IBlock {
		BlockType Type { get; }
	}
}
