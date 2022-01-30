﻿using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Runtime.InteropServices;

public static class SaveGame {

	public static SaveV1 SaveInstance { get; set; }
#if UNITY_EDITOR
	public const bool CompressSaveGame = false;
	public static Formatting JsonFormatting => Formatting.Indented;
#else
	public const bool CompressSaveGame = true;
	public static Formatting JsonFormatting => Formatting.None;
#endif

	public static void GameStartUp() {
		(bool integrityOkay, bool notFound) status = Game.VerifyIntegrity<SaveV1>(GamePaths.SaveGamePath, CompressSaveGame);
		if (status.notFound) {
			try {
				if(Game.CreateFile(GamePaths.SaveGamePath)) {
					Game.WriteToFile(JsonConvert.SerializeObject(CreateFreshSaveGame(), JsonFormatting), GamePaths.SaveGamePath, CompressSaveGame);
					Logger.Log(Channel.SaveGame, "SaveGame file has been created.");
				} else {
					throw new Exception();
				}
			} catch(Exception e) {
				Logger.LogError("Failed to create a fresh SaveGame file.", e);
			}
		} else if(status.integrityOkay == false) {
			Logger.Log(Channel.SaveGame, "SaveGame file seems to be corrupted. Continue to create a new SaveGame file.");
			Game.CreateBackupOfFile(GamePaths.SaveGamePath);
			Game.DeleteFile(GamePaths.SaveGamePath);
			Game.CreateFile(GamePaths.SaveGamePath);
			Game.WriteToFile(JsonConvert.SerializeObject(CreateFreshSaveGame(), JsonFormatting), GamePaths.SaveGamePath, CompressSaveGame);
		}

		LoadGame();
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
		try {
			SaveInstance.LastModified = DateTime.Now;
			var json = JsonConvert.SerializeObject(SaveInstance, JsonFormatting, new JsonSerializerSettings() {
				CheckAdditionalContent = false,
				NullValueHandling = NullValueHandling.Include,
				MissingMemberHandling = MissingMemberHandling.Ignore,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
			});
			Game.WriteToFile(json, GamePaths.SaveGamePath, CompressSaveGame);

			Logger.Log(Channel.SaveGame, "Game has been saved.");
		} catch(Exception e) {
			Logger.LogError("Something went wrong while saving to the SaveGame file.", e);
		}
	}

	public static void LoadGame() {
		try {
			JObject json = JObject.Parse(Game.ReadFromFile(GamePaths.SaveGamePath, CompressSaveGame));
			int version = json[nameof(ISaveGame.SaveGameVersion)].Value<int>();
			Logger.Log(Channel.SaveGame, "Detected SaveGame version: " + version);

			switch (version) {
				case 1: {
						SaveInstance = json.ToObject<SaveV1>();
						SaveInstance.Worlds.AddRange(CheckAddWorlds());
						Logger.Log(Channel.SaveGame, "SaveGame has been loaded.");
					}
					break;

				default:
					Logger.Log(Channel.SaveGame, $"Current SaveGame: v{version} has no compatibility with the game!");
					SaveInstance = CreateFreshSaveGame();
					Save();
					break;
			}
		} catch (Exception e) {
			Logger.LogError("Something went wrong loading the SaveGame file.", e);
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