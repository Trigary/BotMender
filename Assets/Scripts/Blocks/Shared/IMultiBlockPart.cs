namespace Blocks.Shared {
	/// <summary>
	/// The base class of placed and live multi block parts.
	/// </summary>
	public interface IMultiBlockPart {
		void Initialize(IMultiBlockParent parent);
	}
}
