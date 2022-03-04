using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class SaveV1 : SaveBase {

	public new string SaveGameVersion => "1.0";

	public List<PlayedLevel> PlayedLevels { get; set; } = new List<PlayedLevel>();

	public CampaignV1 CurrentCampaign {
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
	[NonSerialized]
	public byte currentSaveSlot = 8;
	public CampaignV1 saveSlot1;
	public CampaignV1 saveSlot2;
	public CampaignV1 saveSlot3;

	public void WriteSaveSlot(byte slot, CampaignV1 campaign) {
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
		GameSaver.Save();
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
		GameSaver.Save();
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
		GameSaver.Save();
	}

	public PlayedLevel GetPlayedLevel(ulong levelId) {
		try {
			return PlayedLevels.Find(l => l.LevelId == levelId);
		} catch {
			throw new Exception("The level with the ID " + levelId + " has not been yet played by the player.");
		}
	}

	[Serializable]
	public class PlayedLevel {
		public readonly ulong LevelId;
		public bool completed;
		public float completionTime;
		public int attempts;
		public DateTime lastAttempt;

		public PlayedLevel(ulong levelId) {
			LevelId = levelId;
		}
	}
}