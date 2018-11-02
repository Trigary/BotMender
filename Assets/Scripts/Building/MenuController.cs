using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Blocks;
using Blocks.Placed;
using DoubleSocket.Utility.BitBuffer;
using Networking;
using Playing.Networking;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Building {
	/// <summary>
	/// Handles all actions in connection with the building mode UI.
	/// </summary>
	public class MenuController : MonoBehaviour {
		private static BitBuffer DefaultStructure {
			get {
				_defaultStructure.SetContents(_defaultStructure.Array);
				return _defaultStructure;
			}
		}
		// ReSharper disable once InconsistentNaming
		private static readonly MutableBitBuffer _defaultStructure = new MutableBitBuffer();

		static MenuController() {
			_defaultStructure.SetContents(new byte[] {2, 240, 129, 94, 35, 128, 15, 252, 146, 0, 124, 32, 152, 4, 224, 3, 195, 36, 0, 31, 40, 38, 1, 248, 192, 49, 13, 192, 7, 146, 105, 0, 190, 208, 19, 7, 240, 133, 96, 60, 128, 47, 12, 131, 1, 124, 161, 24, 2, 0, 4, 63, 0, 0, 32, 8, 138, 0, 0, 193, 49, 9, 0, 8, 146, 12, 1, 192, 240, 43, 6, 0, 134, 163, 72, 0, 80, 20, 133, 0, 132, 160, 215, 8, 32, 4, 63, 32, 0, 33, 8, 34, 1, 8, 193, 16, 9, 64, 8, 138, 72, 0, 66, 112, 68, 3, 16, 130, 36, 27, 128, 48, 244, 244, 1, 132, 33, 8, 15, 32, 12, 67, 96, 0, 97, 40, 2});
		}



		[SerializeField]
		private Text _idText;
		[SerializeField]
		private Text _addressText;
		[SerializeField]
		private Dropdown _blockSelection;
		[SerializeField]
		private Text _displayText;

		private BuildingController _buildingController;
		private EditableStructure _structure;
		private int _id;

		private void Awake() {
			_buildingController = GetComponent<BuildingController>();
			_structure = GetComponent<EditableStructure>();

			_blockSelection.AddOptions(((BlockType[])Enum.GetValues(typeof(BlockType)))
					.Where(type => type != BlockType.Mainframe)
					.Select(type => type.ToString())
					.ToList());
			SelectBlock();
			ResetStructure();
		}



		public void ResetStructure() {
			string file = GetFilePath();
			BitBuffer data;
			if (File.Exists(file)) {
				MutableBitBuffer temp = new MutableBitBuffer();
				temp.SetContents(File.ReadAllBytes(file));
				data = temp;
			} else {
				data = DefaultStructure;
			}

			Assert.IsTrue(_structure.Deserialize(data), "Failed to load the structure.");
		}

		public void SaveStructure() {
			if (ValidateStructure()) {
				Directory.CreateDirectory(GetDirectoryPath());
				File.WriteAllBytes(GetFilePath(), SerializeStructure().Array);
			}
		}

		private string GetDirectoryPath() {
			return Path.Combine(Application.persistentDataPath, "structures");
		}

		private string GetFilePath() {
			return Path.Combine(GetDirectoryPath(), _id.ToString());
		}



		public void NextStructure() {
			SetSelectedStructure((_id + 1) % 10);
		}

		public void PreviousStructure() {
			SetSelectedStructure(_id == 0 ? 9 : _id - 1);
		}

		private void SetSelectedStructure(int id) {
			_id = id;
			_idText.text = id.ToString();
			ResetStructure();
		}



		public void PlayAsHost() {
			if (ValidateStructure()) {
				NetworkServer.Start();
				NetworkServer.ConnectHandler = client => { };
				_displayText.text = "Initializing as host...";
				InitializeNetworkClient(IPAddress.Loopback);
			}
		}

		public void PlayAsClient() {
			if (ValidateStructure()) {
				IPAddress address = IPAddress.Loopback;
				if (_addressText.text.Length == 0 || IPAddress.TryParse(_addressText.text, out address)) {
					_displayText.text = "Initializing as client...";
					InitializeNetworkClient(address);
				} else {
					_displayText.text = "Error: unable to parse the specified address";
				}
			}
		}

		private void InitializeNetworkClient(IPAddress address) {
			NetworkClient.Start(address, (success, connectionFailure,
					authenticationFailure, timeout, connectionLost) => {
				if (!success) {
					_displayText.text = "Error: networking failed";
					Debug.Log($"Networking failed: SocketError:{connectionFailure} | " +
							$"Auth:{authenticationFailure} | Timeout:{timeout} | ConnLost:{connectionLost}");
					if (NetworkServer.Initialized) {
						NetworkServer.Stop();
					}
					return;
				}

				_displayText.text = "Loading world...";
				byte[] structure = SerializeStructure().Array;

				// ReSharper disable once ConvertToLocalFunction
				UnityAction<Scene, LoadSceneMode> action = null;
				action = (scene, mode) => {
					PlayingPlayerInitializer.OnNetworkingInitialized(structure);
					SceneManager.sceneLoaded -= action;
				};

				SceneManager.sceneLoaded += action;
				SceneManager.LoadScene("Playing");
			});
		}



		public void SelectBlock() {
			Assert.IsTrue(Enum.TryParse(_blockSelection.options[_blockSelection.value].text, out BlockType type),
					"Failed to parse the selected block type.");
			_buildingController.BlockType = type;
		}



		private BitBuffer SerializeStructure() {
			BitBuffer buffer = new MutableBitBuffer((RealPlacedBlock.SerializedBitsSize
					* _structure.RealBlockCount + 7) / 8);
			_structure.Serialize(buffer);
			return buffer;
		}

		private bool ValidateStructure() {
			EditableStructure.Errors errors = _structure.GetStructureErrors();
			IDictionary<BlockPosition, IPlacedBlock> temp = _structure.GetNotConnectedBlocks();
			bool notConnected = temp != null && temp.Count > 0;
			if (errors == EditableStructure.Errors.None && !notConnected) {
				return true;
			}

			_displayText.text = "Error: " + errors + (notConnected ? ", not connected blocks" : "");
			return false;
		}
	}
}
