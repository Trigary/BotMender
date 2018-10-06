using System;
using System.Linq;
using JetBrains.Annotations;
using Networking;
using Structures;
using UnityEngine.Assertions;

namespace Playing {
	/// <summary>
	/// A cache for CompleteStructure instances and any extra data associated with them.
	/// </summary>
	public static class BotCache {
		public static int Count { get; private set; }
		private static readonly int ExtraCount = Enum.GetValues(typeof(Extra)).Length;
		private static readonly CompleteStructure[] Cache = new CompleteStructure[NetworkUtils.MaxBotCount + 1];
		private static readonly object[,] ExtraData = new object[Cache.Length, ExtraCount];



		/// <summary>
		/// Returns the structure with the specified player/bot id
		/// or null if no structure was registered with that id.
		/// </summary>
		public static CompleteStructure Get(byte id) {
			return Cache[id];
		}

		/// <summary>
		/// Returns whether the 'out' parameter is the structure with the specified player/bot id
		/// or null if no structure was registered with the id.
		/// </summary>
		// ReSharper disable once AnnotateCanBeNullParameter
		public static bool TryGet(byte id, out CompleteStructure structure) {
			structure = Cache[id];
			return structure != null;
		}



		/// <summary>
		/// Returns the extra data associated with the speicified player/bot id and owner.
		/// Returns null if the associated value is null or if no value was set.
		/// </summary>
		public static T GetExtra<T>(byte id, Extra owner) {
			return (T)ExtraData[id, (int)owner];
		}

		/// <summary>
		/// Returns the extra data associated with the speicified player/bot id and owner
		/// or the value returned by the specified supplier if the extra data is null.
		/// If the provider was called then the returned value will be stored in this cache.
		/// </summary>
		public static T GetExtra<T>(byte id, Extra owner, Func<T> defaultSupplier) {
			object value = ExtraData[id, (int)owner];
			if (value == null) {
				value = defaultSupplier();
				ExtraData[id, (int)owner] = value;
			}
			return (T)value;
		}

		/// <summary>
		/// Returns the extra data associated with the speicified player/bot id and owner
		/// while also setting it to null afterward.
		/// Returns null if the associated value is null or if no value was set.
		/// </summary>
		public static T TakeExtra<T>(byte id, Extra owner) {
			object value = ExtraData[id, (int)owner];
			ExtraData[id, (int)owner] = null;
			return (T)value;
		}



		/// <summary>
		/// Registers the specified structure in this cache.
		/// Should only be called by the CompleteStructure class.
		/// </summary>
		public static void Add(CompleteStructure structure) {
			Assert.IsNull(Cache[structure.Id], "A structure with that id is already registered.");
			Cache[structure.Id] = structure;
			Count++;
		}

		/// <summary>
		/// Remove the structure with the specified player/bot id and returns it.
		/// Fails if the structure isn't registered.
		/// Should only be called by the CompleteStructure class.
		/// </summary>
		public static void Remove(byte id) {
			CompleteStructure structure = Cache[id];
			Assert.IsNotNull(structure, "No structure with that id is registered.");
			Cache[id] = null;
			Count--;
			for (int i = 0; i < ExtraCount; i++) {
				ExtraData[id, i] = null;
			}
		}

		/// <summary>
		/// Sets the extra data associated with the speicified player/bot id and owner.
		/// In order to simulate the removal of the data, set associate the keys with null.
		/// </summary>
		public static void SetExtra(byte id, Extra owner, [CanBeNull] object value) {
			ExtraData[id, (int)owner] = value;
		}



		/// <summary>
		/// Clears the whole cache: removes all registered structures.
		/// </summary>
		public static void Clear() {
			for (int id = 0; id < Cache.Length; id++) {
				if (Cache[id] != null) {
					Cache[id] = null;
					for (int i = 0; i < ExtraCount; i++) {
						ExtraData[id, i] = null;
					}
				}
			}
			Count = 0;
		}

		/// <summary>
		/// Clears all extra data associated with the specified owner.
		/// </summary>
		public static void ClearExtra(Extra owner) {
			for (int id = 0; id < Cache.Length; id++) {
				ExtraData[id, (int)owner] = null;
			}
		}



		/// <summary>
		/// Executes the specified action for all registered CompleteStructures.
		/// </summary>
		public static void ForEach(Action<CompleteStructure> action) {
			foreach (CompleteStructure structure in Cache.Where(structure => structure != null)) {
				action(structure);
			}
		}

		/// <summary>
		/// Executes the specified action for all keys which are associated with CompleteStructures.
		/// </summary>
		public static void ForEachId(Action<byte> action) {
			for (byte id = 0; id < Cache.Length; id++) {
				if (Cache[id] != null) {
					action(id);
				}
			}
		}



		/// <summary>
		/// The enum values represent classes which can associate extra data with the CompleteStructure instances.
		/// </summary>
		public enum Extra : byte {
			NetworkedPhysics,
			NetworkedBotController
		}
	}
}
