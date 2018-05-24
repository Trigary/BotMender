using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts.Utilities {
	public class CircularList<T> : IEnumerable<T> {
		private readonly List<T> _list = new List<T>();
		private int _index;

		public void Add(T element) {
			_list.Add(element);
		}

		public bool Remove(T element) {
			bool removed = _list.Remove(element);
			_index %= _list.Count;
			return removed;
		}

		public int Count() {
			return _list.Count;
		}

		public T Next() {
			T element = _list[_index++];
			_index %= _list.Count;
			return element;
		}

		public void TrimExcess() {
			_list.TrimExcess();
		}



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

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
