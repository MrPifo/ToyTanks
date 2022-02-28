using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using BayatGames.SaveGameFree;
using BayatGames.SaveGameFree.Serializers;

public static class GameSaver {

	public static SaveV1 SaveInstance { get; set; }
	public static string VersionNumber;
	public static bool EncodeSaveGame = true;

	#region API
	public static void GameStartUp() {
		SaveGame.Serializer = new SaveGameJsonSerializer();
		SaveGame.LogError = true;
#if UNITY_EDITOR
		EncodeSaveGame = false;
#endif
		SaveGame.Encode = EncodeSaveGame;

		LoadGame();
	}

	private static SaveV1 CreateFreshSaveGame() {
		var save = new SaveV1() {
			Worlds = new List<SaveV1.World>()
		};
		foreach(var en in Game.GetWorlds) {
			var levels = new SaveV1.Level[Game.LevelCount(en.WorldType)];
			var gameLevels = Game.GetLevels(en.WorldType);
			for(int i = 0; i < gameLevels.Length; i++) {
				levels[i] = new SaveV1.Level() {
					IsUnlocked = false,
					LevelId = gameLevels[i].LevelId
				};
			}

			save.Worlds.Add(new SaveV1.World() {
				world = en.WorldType,
				levels = levels
			});
		}
		return save;
	}

	public static void CreateFreshCampaign(CampaignV1.Difficulty difficulty, byte saveSlot) {
		CampaignV1 campaign;
		Logger.Log(Channel.SaveGame, "Creating new campaign with difficulty " + difficulty.ToString() + " on save slot " + saveSlot);
		switch(difficulty) {
			case CampaignV1.Difficulty.Easy:
				campaign = new CampaignV1() {
					difficulty = CampaignV1.Difficulty.Easy,
					levelId = 1,
					lives = 0,
				};
				break;
			case CampaignV1.Difficulty.Medium:
				campaign = new CampaignV1() {
					difficulty = CampaignV1.Difficulty.Medium,
					levelId = 1,
					lives = 4,
				};
				break;
			case CampaignV1.Difficulty.Hard:
				campaign = new CampaignV1() {
					difficulty = CampaignV1.Difficulty.Hard,
					levelId = 1,
					lives = 3,
				};
				break;
			case CampaignV1.Difficulty.Original:
				campaign = new CampaignV1() {
					difficulty = CampaignV1.Difficulty.Original,
					levelId = 1,
					lives = 3,
				};
				break;
			default:
				throw new NotImplementedException("Failed creating campaign with difficulty: " + difficulty.ToString());
		}
		SaveInstance.WriteSaveSlot(saveSlot, campaign);
	}

	public static void Save() {
		try {
			SaveInstance.LastModified = DateTime.Now;
			SaveGame.Save("SaveGame", SaveInstance);

			Logger.Log(Channel.SaveGame, "Game has been saved.");
		} catch(Exception e) {
			Logger.LogError("Something went wrong while saving to the SaveGame file.", e);
		}
	}

	public static void UpdateCampaign(ulong levelId, byte lives, int score, float time, byte saveSlot = 99) {
		if(saveSlot == 99) {
			SaveInstance.UpdateSaveSlot(SaveInstance.currentSaveSlot, levelId, lives, score, time);
		} else {
			SaveInstance.UpdateSaveSlot(saveSlot, levelId, lives, score, time);
		}
		Save();
	}

	public static CampaignV1 GetCampaign(byte saveSlot) {
		switch(saveSlot) {
			case 0:
				return SaveInstance.saveSlot1;
			case 1:
				return SaveInstance.saveSlot2;
			case 2:
				return SaveInstance.saveSlot3;
			case 8:
				Debug.LogWarning("No active SaveSlot selected! If this is message shows up during Level-Scene startup everything is okay.");
				return null;
			default:
				throw new NotImplementedException("SaveSlot " + saveSlot + " is not available.");
		}
	}

	public static void UnlockLevel(byte saveSlot, ulong levelId) {
		GetWorld(GetCampaign(saveSlot).CurrentWorld).levels.Where(l => l.LevelId == levelId).First().IsUnlocked = true;
	}

	#endregion

	#region Internal
	private static List<SaveV1.World> CheckAddWorlds() {
		var worlds = new List<SaveV1.World>();
		foreach(var en in Game.GetWorlds) {
			if(GetWorld(en.WorldType) == null) {
				var levels = new SaveV1.Level[Game.LevelCount(en.WorldType)];
				var gameLevels = Game.GetLevels(en.WorldType);
				for(int i = 0; i < gameLevels.Length; i++) {
					levels[i] = new SaveV1.Level() {
						IsUnlocked = false,
						LevelId = gameLevels[i].LevelId
					};
				}

				worlds.Add(new SaveV1.World() {
					world = en.WorldType,
					levels = levels
				});
			}
		}
		return worlds;
	}

	private static void LoadGame() {
		// Check GameSave Version Number
		SaveGame.Encode = false;	// is not encoded
		if(SaveGame.TryLoad(nameof(VersionNumber), out VersionNumber) && VersionNumber != "") {
			Logger.Log(Channel.SaveGame, "Detected save game version " + VersionNumber);
		} else {
			Logger.Log(Channel.SaveGame, "Failed to identify save game version.");
			VersionNumber = "1.0";    // Needs to be updated to newest version if updated.
			// Setting VersionNumber to highest default
			SaveGame.Save(nameof(VersionNumber), "1.0");
		}
		SaveGame.Encode = EncodeSaveGame;

		switch(VersionNumber) {
			case "1.0":
				if(SaveGame.TryLoad("SaveGame", out SaveV1 _saveInstance)) {
					SaveInstance = _saveInstance;
				} else {
					Logger.Log(Channel.SaveGame, "No compatible SaveGame has been found. Creating new one.");
					SaveInstance = CreateFreshSaveGame();
					Save();
				}
				break;
			default:
				SaveInstance = CreateFreshSaveGame();
				Save();
				break;
		}
	}


	#endregion

	#region InfoAPI
	// Version dependent Helper functions
	public static bool HasGameBeenCompletedOnce => SaveInstance.GameCompletedOnce;
	public static SaveV1.World GetWorld(WorldTheme world) => SaveInstance.Worlds.Find(w => w.world == world);
	public static SaveV1.Level GetLevel(WorldTheme world, ulong levelId) => GetWorld(world).levels.Where(l => l.LevelId == levelId).FirstOrDefault();
	public static int UnlockedLevelCount(WorldTheme world) => GetWorld(world).levels.Count(l => l.IsUnlocked == true);
	public static bool IsLevelUnlocked(WorldTheme world, ulong levelId) => GetLevel(world, levelId).IsUnlocked;
	#endregion
}