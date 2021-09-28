using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;


public static class SaveGame {

	public static SaveV1 SaveInstance { get; set; }
	public static Formatting JsonFormatting => Formatting.Indented;

	public static void GameStartUp() {
		if(File.Exists(GamePaths.SaveGamePath) == false) {
			SaveInstance = CreateFreshSaveGame();
			var json = JsonConvert.SerializeObject(SaveInstance, JsonFormatting);
			using(File.Create(GamePaths.SaveGamePath)) {}
			File.WriteAllText(GamePaths.SaveGamePath, json);
		} else {
			LoadGame();
		}
	}

	public static SaveV1 CreateFreshSaveGame() {
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

	public static void CreateFreshCampaign(Campaign.Difficulty difficulty, byte saveSlot) {
		Campaign campaign;
		Debug.Log("Creating new campaign with difficulty " + difficulty.ToString() + " on save slot " + saveSlot);
		switch(difficulty) {
			case Campaign.Difficulty.Easy:
				campaign = new Campaign() {
					difficulty = Campaign.Difficulty.Easy,
					levelId = 1,
					lives = 0,
				};
				break;
			case Campaign.Difficulty.Medium:
				campaign = new Campaign() {
					difficulty = Campaign.Difficulty.Medium,
					levelId = 1,
					lives = 4,
				};
				break;
			case Campaign.Difficulty.Hard:
				campaign = new Campaign() {
					difficulty = Campaign.Difficulty.Hard,
					levelId = 1,
					lives = 2,
				};
				break;
			case Campaign.Difficulty.HardCore:
				campaign = new Campaign() {
					difficulty = Campaign.Difficulty.HardCore,
					levelId = 1,
					lives = 3,
				};
				break;
			default:
				throw new NotImplementedException("Failed creating campaign with difficulty: " + difficulty.ToString());
		}
		SaveInstance.WriteSaveSlot(saveSlot, campaign);
	}

	public static List<SaveV1.World> CheckAddWorlds() {
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

	public static void Save() {
		if(File.Exists(GamePaths.SaveGamePath)) {
			var json = JsonConvert.SerializeObject(SaveInstance, JsonFormatting);
			File.WriteAllText(GamePaths.SaveGamePath, json);
		}
	}

	public static void LoadGame() {
		if(File.Exists(GamePaths.SaveGamePath)) {
			var txt = File.ReadAllText(GamePaths.SaveGamePath);
			var json = JObject.Parse(txt);
			var version = json[nameof(ISaveGame.SaveGameVersion)].Value<int>();
			Debug.Log("Detected SaveGame version: " + version);

			switch(version) {
				case 1: {
					SaveInstance = json.ToObject<SaveV1>();
					SaveInstance.Worlds.AddRange(CheckAddWorlds());
					Save();
				}
				break;

				default:
					Debug.LogError($"Current SaveGame: v{version} has no compatibility with the game!");
					SaveInstance = CreateFreshSaveGame();
					Save();
					break;
			}
		}
	}

	public static void UpdateCampaign(ulong levelId, byte lives, short score, float time, byte saveSlot = 99) {
		if(saveSlot == 99) {
			SaveInstance.UpdateSaveSlot(SaveInstance.currentSaveSlot, levelId, lives, score, time);
		} else {
			SaveInstance.UpdateSaveSlot(saveSlot, levelId, lives, score, time);
		}
		Save();
	}

	public static Campaign GetCampaign(byte saveSlot) {
		switch(saveSlot) {
			case 0: return SaveInstance.saveSlot1;
			case 1: return SaveInstance.saveSlot2;
			case 2: return SaveInstance.saveSlot3;
			default:
				throw new NotImplementedException("SaveSlot " + saveSlot + " is not available.");
		}
	}

	public static void UnlockLevel(byte saveSlot, ulong levelId) {
		GetWorld(GetCampaign(saveSlot).CurrentWorld).levels.Where(l => l.LevelId == levelId).First().IsUnlocked = true;
	}

	// Version dependent Helper functions
	public static bool HasGameBeenCompletedOnce => SaveInstance.GameCompletedOnce;
	public static SaveV1.World GetWorld(Worlds world) => SaveInstance.Worlds.Find(w => w.world == world);
	public static SaveV1.Level GetLevel(Worlds world, ulong levelId) => GetWorld(world).levels.Where(l => l.LevelId == levelId).First();
	public static int UnlockedLevelCount(Worlds world) => GetWorld(world).levels.Count(l => l.IsUnlocked == true);
	public static bool IsLevelUnlocked(Worlds world, ulong levelId) => GetLevel(world, levelId).IsUnlocked;

	public class Campaign {

		public enum Difficulty { Easy, Medium, Hard, HardCore }

		[JsonIgnore] public Worlds CurrentWorld => Game.GetWorld(levelId).WorldType;
		public Difficulty difficulty;
		public ulong levelId;
		public byte lives;
		public short score;
		public float time;

		[JsonIgnore] public float PrettyTime => Mathf.Round(time * 100f) / 100f;
	}
}