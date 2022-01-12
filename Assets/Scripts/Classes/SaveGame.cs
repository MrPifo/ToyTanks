using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Runtime.InteropServices;

public static class SaveGame {

	public static SaveV1 SaveInstance { get; set; }
	public static Formatting JsonFormatting => Formatting.Indented;

	private static void WriteSaveGame(string json) {
		using(StreamWriter sw = new StreamWriter(GamePaths.SaveGamePath, false, System.Text.Encoding.UTF8)) {
			sw.Write(json);
			sw.Flush();
		}
	}
	private static string ReadSaveGame() {
		using(StreamReader sr = new StreamReader(GamePaths.SaveGamePath, System.Text.Encoding.Unicode)) {
			return sr.ReadToEnd();
		}
	}
	public static void GameStartUp() {
		if (ValidateSaveGame() == false) {
			SaveInstance = CreateFreshSaveGame();
			WriteSaveGame(JsonConvert.SerializeObject(SaveInstance, JsonFormatting));

			Logger.Log(Channel.SaveGame, "SaveGame file has been created.");
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
		Logger.Log(Channel.SaveGame, "Creating new campaign with difficulty " + difficulty.ToString() + " on save slot " + saveSlot);
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
					lives = 3,
				};
				break;
			case Campaign.Difficulty.Original:
				campaign = new Campaign() {
					difficulty = Campaign.Difficulty.Original,
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
		if(ValidateSaveGame()) {
			try {
				var json = JsonConvert.SerializeObject(SaveInstance, JsonFormatting);
				WriteSaveGame(json);

				Logger.Log(Channel.SaveGame, "Game has been saved.");
			} catch (Exception e) {
				Logger.LogError(Channel.SaveGame, "Something went wrong saving to the SaveGame file.", e);
			}
		} else {
			Logger.Log(Channel.SaveGame, "No Savegame file has been found.");
        }
	}

	public static void LoadGame() {
		if(ValidateSaveGame()) {
			try {
				Logger.Log(Channel.SaveGame, "Loading SaveGame.");
				JObject json = JObject.Parse(ReadSaveGame());
				int version = json[nameof(ISaveGame.SaveGameVersion)].Value<int>();
				Logger.Log(Channel.SaveGame, "Detected SaveGame version: " + version);

				switch (version) {
					case 1: {
							SaveInstance = json.ToObject<SaveV1>();
							SaveInstance.Worlds.AddRange(CheckAddWorlds());
							Save();
						}
						break;

					default:
						Logger.Log(Channel.SaveGame, Priority.Error, $"Current SaveGame: v{version} has no compatibility with the game!");
						SaveInstance = CreateFreshSaveGame();
						Save();
						break;
				}
			} catch (Exception e) {
				Logger.LogError(Channel.SaveGame, "Something went wrong loading the SaveGame file.", e);
			}
		} else {
			Logger.Log(Channel.SaveGame, "No SaveGame file has been found.");
        }
	}

	public static bool ValidateSaveGame() {
		if(File.Exists(GamePaths.SaveGamePath) == false) {
			Logger.Log(Channel.SaveGame, "No Savegame file has been found.");
			return false;
		}
		FileInfo info = new FileInfo(GamePaths.SaveGamePath);
		if (info.Length == 0) {
			Logger.Log(Channel.SaveGame, "Savegame file is empty.");
			return false;
		}
		try {
			JObject json = JObject.Parse(ReadSaveGame());
			return true;
		} catch (Exception e) {
			Logger.LogError(Channel.SaveGame, "Failed to parse Savegame file.", e);
			return false;
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
	public static SaveV1.Level GetLevel(Worlds world, ulong levelId) => GetWorld(world).levels.Where(l => l.LevelId == levelId).FirstOrDefault();
	public static int UnlockedLevelCount(Worlds world) => GetWorld(world).levels.Count(l => l.IsUnlocked == true);
	public static bool IsLevelUnlocked(Worlds world, ulong levelId) => GetLevel(world, levelId).IsUnlocked;

	public class Campaign {

		public enum Difficulty { Easy, Medium, Hard, Original }

		[JsonIgnore] public Worlds CurrentWorld => Game.GetWorld(levelId).WorldType;
		public Difficulty difficulty;
		public ulong levelId;
		public byte lives;
		public int score;
		public float time;
		public float liveGainChance;

		[JsonIgnore] public float PrettyTime => Mathf.Round(time * 100f) / 100f;
	}
}