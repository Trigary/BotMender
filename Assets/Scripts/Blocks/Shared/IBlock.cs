namespace Blocks.Shared {
	/// <summary>
	/// The base class of placed and live blocks.
	/// </summary>
	public interface IBlock {
		BlockSides ConnectSides { get; }
		BlockPosition Position { get; }
	}
}
