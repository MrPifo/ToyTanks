using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

[Serializable]
public class SaveV1 : ISaveGame {

	public int SaveGameVersion => 1;
	public string LastPlayedUtcTimestamp { get; set; }
	public bool GameCompletedOnce { get; set; }

	public List<World> Worlds { get; set; }

	[JsonIgnore] public SaveGame.Campaign CurrentCampaign {
		get {
			switch(currentSaveSlot) {
				case 0:
					return saveSlot1;
				case 1:
					return saveSlot2;
				case 2:
					return saveSlot3;
				default:
					throw new NotImplementedException("Save Slot " + currentSaveSlot + " is not available");
			}
		}
	}
	[JsonIgnore] public byte currentSaveSlot;
	public SaveGame.Campaign saveSlot1;
	public SaveGame.Campaign saveSlot2;
	public SaveGame.Campaign saveSlot3;

	public void WriteSaveSlot(byte slot, SaveGame.Campaign campaign) {
		switch(slot) {
			case 0:
				saveSlot1 = campaign;
				break;
			case 1:
				saveSlot2 = campaign;
				break;
			case 2:
				saveSlot3 = campaign;
				break;
			default:
				throw new NotImplementedException("Save Slot " + currentSaveSlot + " is not available");
		}
		SaveGame.Save();
	}

	public void UpdateSaveSlot(byte slot, ulong levelId, byte lives, int score, float time) {
		switch(slot) {
			case 0:
				saveSlot1.levelId = levelId;
				saveSlot1.lives = lives;
				saveSlot1.score = score;
				saveSlot1.time = time;
				break;
			case 1:
				saveSlot2.levelId = levelId;
				saveSlot2.lives = lives;
				saveSlot2.score = score;
				saveSlot2.time = time;
				break;
			case 2:
				saveSlot3.levelId = levelId;
				saveSlot3.lives = lives;
				saveSlot3.score = score;
				saveSlot3.time = time;
				break;
			default:
				throw new NotImplementedException("Save Slot " + currentSaveSlot + " is not available");
		}
		SaveGame.Save();
	}

	public void WipeSlot(byte slot) {
		switch(slot) {
			case 0:
				saveSlot1 = null;
				break;
			case 1:
				saveSlot2 = null;
				break;
			case 2:
				saveSlot3 = null;
				break;
			default:
				throw new NotImplementedException("Save Slot " + currentSaveSlot + " is not available");
		}
		SaveGame.Save();
	}

	[Serializable]
	public class World {
		[JsonConverter(typeof(StringEnumConverter))]
		public Worlds world;
		public Level[] levels;
	}

	[Serializable]
	public class Level {
		public ulong LevelId;
		public bool IsUnlocked;
	}
}