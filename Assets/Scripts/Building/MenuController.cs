using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Blocks;
using Blocks.Placed;
using DoubleSocket.Utility.BitBuffer;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Building {
	/// <summary>
	/// Handles all actions in connection with the building mode UI.
	/// </summary>
	public class MenuController : MonoBehaviour {
		public static BitBuffer ExampleStructure {
			get {
				_exampleStructure.SetContents(_exampleStructure.Array);
				return _exampleStructure;
			}
		}
		// ReSharper disable once InconsistentNaming
		private static readonly MutableBitBuffer _exampleStructure = new MutableBitBuffer();

		static MenuController() {
			_exampleStructure.SetContents(new byte[] {2, 240, 129, 94, 35, 128, 15, 252, 146, 0, 124, 32, 152, 4, 224, 3, 195, 36, 0, 31, 40, 38, 1, 248, 192, 49, 13, 192, 7, 146, 105, 0, 190, 208, 19, 7, 240, 133, 96, 60, 128, 47, 12, 131, 1, 124, 161, 24, 2, 0, 4, 63, 0, 0, 32, 8, 138, 0, 0, 193, 49, 9, 0, 8, 146, 12, 1, 192, 240, 43, 6, 0, 134, 163, 72, 0, 80, 20, 133, 0, 132, 160, 215, 8, 32, 4, 63, 32, 0, 33, 8, 34, 1, 8, 193, 16, 9, 64, 8, 138, 72, 0, 66, 112, 68, 3, 16, 130, 36, 27, 128, 48, 244, 244, 1, 132, 33, 8, 15, 32, 12, 67, 96, 0, 97, 40, 2});
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
				data = ExampleStructure;
			}

			Assert.IsTrue(_structure.Deserialize(data), "Failed to load the structure.");
			//TODO center structure
		}

		public void SaveStructure() {
			if (ValidateStructure()) {
				BitBuffer buffer = new MutableBitBuffer((RealPlacedBlock.SerializedBitsSize
						* _structure.RealBlockCount + 7) / 8);
				_structure.Serialize(buffer);
				Directory.CreateDirectory(GetDirectoryPath());
				File.WriteAllBytes(GetFilePath(), buffer.Array);
			}
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
			if (!ValidateStructure()) {
				return;
			}

			//TODO pass data to the next scene?
		}

		public void PlayAsClient() {
			if (!ValidateStructure()) {
				return;
			}

			if (!IPAddress.TryParse(_addressText.text, out IPAddress address)) {
				_displayText.text = "Error: unable to parse the specified address";
				return;
			}

			//TODO pass data to the next scene?
		}



		public void SelectBlock() {
			Assert.IsTrue(Enum.TryParse(_blockSelection.options[_blockSelection.value].text, out BlockType type),
					"Failed to parse the selected block type.");
			_buildingController.BlockType = type;
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

		private string GetDirectoryPath() {
			return Path.Combine(Application.persistentDataPath, "structures");
		}

		private string GetFilePath() {
			return Path.Combine(GetDirectoryPath(), _id.ToString());
		}


		/*EditableStructure.Errors errors = _structure.GetStructureErrors();
			if (errors != EditableStructure.Errors.None) {
				Debug.Log("Structure error: " + errors);
				return;
			}

			IDictionary<BlockPosition, IPlacedBlock> notConnected = _structure.GetNotConnectedBlocks();
			Assert.IsNotNull(notConnected, "The lack of the presence of the Mainframe was not shown among the errors.");
			if (notConnected.Count != 0) {
				Debug.Log("Structure error: not connected blocks");
				return;
			}

			BitBuffer someBuffer = new MutableBitBuffer((RealPlacedBlock.SerializedBitsSize
					* _structure.RealBlockCount + 7) / 8);
			_structure.Serialize(someBuffer);
			Debug.Log("Structure: " + string.Join(", ", someBuffer.Array));
			CompleteStructure complete = CompleteStructure.Create(someBuffer, 1);
			Assert.IsNotNull(complete, "Own CompleteStructure creation mustn't fail.");

			complete.transform.position = new Vector3(0, 10, 0);
			complete.gameObject.AddComponent<LocalBotController>();
			_camera.gameObject.AddComponent<PlayingCameraController>().Initialize(complete);

			Destroy(_camera.gameObject.GetComponent<BuildingCameraController>());
			Destroy(gameObject);

			CompleteStructure otherStructure = CompleteStructure.Create(ExampleStructure, 2);
			Assert.IsNotNull(otherStructure, "Other CompleteStructure creation mustn't fail.");
			otherStructure.transform.position = new Vector3(150, 5, 150);*/
	}
}
