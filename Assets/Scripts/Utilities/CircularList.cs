using System.Collections;
using System.Collections.Generic;

namespace Utilities {
	/// <summary>
	/// A list in which after the last element, the first one comes again.
	/// Enumeration loops over each element only once.
	/// </summary>
	public class CircularList<T> : IEnumerable<T> {
		public int Count => _list.Count;
		private readonly List<T> _list = new List<T>();
		private int _index;

		/// <summary>
		/// Adds an element to this list.
		/// </summary>
		public void Add(T element) {
			_list.Add(element);
		}

		/// <summary>
		/// Tries to remove the specified element from this list, returning whether it was successful.
		/// </summary>
		public bool Remove(T element) {
			bool removed = _list.Remove(element);
			_index %= _list.Count;
			return removed;
		}

		/// <summary>
		/// Gets the next element.
		/// </summary>
		public T Next() {
			T element = _list[_index++];
			_index %= _list.Count;
			return element;
		}

		/// <summary>
		/// Makes sure the list uses no more memory than it needs to.
		/// Should only be called after all elements have been added.
		/// </summary>
		public void TrimExcess() {
			_list.TrimExcess();
		}



		/// <summary>
		/// Returns the enumerator which can be used to loop over all elements of this collection once.
		/// </summary>
		public IEnumerator<T> GetEnumerator() {
			if (_list.Count == 0) {
				yield break;
			}

			int originalIndex = _index;
			yield return Next();
			while (_index != originalIndex) {
				yield return Next();
			}
		}

		/// <summary>
		/// Returns the enumerator which can be used to loop over all elements of this collection once.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
