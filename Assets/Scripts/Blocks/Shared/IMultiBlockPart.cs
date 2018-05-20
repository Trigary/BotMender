namespace Assets.Scripts.Blocks.Shared {
	/// <summary>
	/// The base class of placed and live mutli block parts.
	/// </summary>
	public interface IMultiBlockPart {
		void Initialize(IMultiBlockParent parent);
	}
}
