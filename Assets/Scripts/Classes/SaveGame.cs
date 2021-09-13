using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class SaveGame {

	public static SaveV1 SaveInstance { get; set; }
	public static Campaign CurrentCampaign => SaveInstance.CurrentCampaign;
	public static string SaveGamePath { get; set; } = Application.persistentDataPath + "/SaveGame.dat";
	public static Formatting JsonFormatting => Formatting.Indented;

	public static void GameStartUp() {
		if(File.Exists(SaveGamePath) == false) {
			SaveInstance = CreateFreshSaveGame();
			var json = JsonConvert.SerializeObject(SaveInstance, JsonFormatting);
			using(File.Create(SaveGamePath)) {}
			File.WriteAllText(SaveGamePath, json);
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

	public static void CreateFreshCampaign(Campaign.Difficulty difficulty, byte startLives) {
		var campaign = new Campaign() {
			difficulty = difficulty,
			levelId = 1,
			lives = startLives,
			score = 0,
			time = 0
		};
		SaveInstance.CurrentCampaign = campaign;
		Save();
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
		if(File.Exists(SaveGamePath)) {
			var json = JsonConvert.SerializeObject(SaveInstance, JsonFormatting);
			File.WriteAllText(SaveGamePath, json);
		}
	}

	public static void LoadGame() {
		if(File.Exists(SaveGamePath)) {
			var txt = File.ReadAllText(SaveGamePath);
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

	public static void UpdateCampaign(int levelId, byte lives, short score, float time) {
		CurrentCampaign.levelId = levelId;
		CurrentCampaign.lives = lives;
		CurrentCampaign.score = score;
		CurrentCampaign.time = time;
		Save();
	}

	// Version dependent Helper functions
	public static SaveV1.World GetWorld(Worlds world) => SaveInstance.Worlds.Find(w => w.world == world);
	public static SaveV1.Level GetLevel(Worlds world, int levelId) => GetWorld(world).levels.Where(l => l.LevelId == levelId).First();

	public static int UnlockedLevelCount(Worlds world) => GetWorld(world).levels.Count(l => l.IsUnlocked == true);

	public static bool IsLevelUnlocked(Worlds world, int levelId) => GetLevel(world, levelId).IsUnlocked;

	public class Campaign {

		public enum Difficulty { Easy, Medium, Hard, Extreme }

		public Worlds CurrentWorld => Game.GetWorld(levelId).WorldType;
		public Difficulty difficulty;
		public int levelId;
		public byte lives;
		public short score;
		public float time;
	}
}