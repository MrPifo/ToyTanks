using System;
using System.IO;
using Newtonsoft.Json;

public class PlayerStats {

	public static PlayerStats Instance { get; set; }
	public int TotalKills { get; set; }
	public int TotalPlaytime { get; set; }
	public int TotalLevelsCompleted { get; set; }
	public int TotalDeaths { get; set; }
	public bool ArcadeCompletedOnce => false;
	public int TotalDistanceTravelled	{ get; set; }
	public bool DifficultyEasyCompleted { get; set; }
	public bool DifficultyMediumCompleted { get; set; }
	public bool DifficultyHardCompleted { get; set; }
	public bool DifficultyOriginalCompleted { get; set; }
	public int TotalBossesKilled { get; set; }
	public int TotalScore { get; set; }
	public DateTime LastModified { get; set; }
	public static string FilePath => GamePaths.PlayerStatsFile;

#if UNITY_EDITOR
	public const bool CompressPlayerStats = false;
	public static Formatting JsonFormatting => Formatting.Indented;
#else
	public const bool CompressPlayerStats = true;
	public static Formatting JsonFormatting => Formatting.None;
#endif

	public static void GameStartup() {
		(bool integrityOkay, bool notFound) status = Game.VerifyIntegrity<PlayerStats>(FilePath, CompressPlayerStats);
		if(status.notFound) {
			try {
				if(Game.CreateFile(FilePath)) {
					Game.WriteToFile(JsonConvert.SerializeObject(new PlayerStats(), JsonFormatting), FilePath, CompressPlayerStats);
					Logger.Log(Channel.PlayerStats, "PlayerStats file has been created.");
				} else {
					throw new Exception();
				}
			} catch(Exception e) {
				Logger.LogError("Failed to create a fresh PlayerStats file.", e);
			}
		} else if(status.integrityOkay == false) {
			Logger.Log(Channel.PlayerStats, "PlayerStats file seems to be corrupted. Continue to create a new PlayerStats file.");
			Game.CreateBackupOfFile(FilePath);
			Game.DeleteFile(FilePath);
			Game.CreateFile(FilePath);
			Game.WriteToFile(JsonConvert.SerializeObject(new PlayerStats(), JsonFormatting), FilePath, CompressPlayerStats);
		}

		LoadPlayerStats();
	}

	public static void LoadPlayerStats() {
		try {
			string json = Game.ReadFromFile(FilePath, CompressPlayerStats);
			Instance = JsonConvert.DeserializeObject<PlayerStats>(json);

			Logger.Log(Channel.PlayerStats, "PlayerStats have been loaded. Last Modification: " + Instance.LastModified.ToShortDateString());
		} catch(Exception e) {
			Logger.LogError("Something went wrong loading the PlayerStats file.", e);
		}
	}

	public static void SavePlayerStats() {
		try {
			Instance.LastModified = DateTime.Now;
			var json = JsonConvert.SerializeObject(Instance, JsonFormatting);
			Game.WriteToFile(json, FilePath, CompressPlayerStats);

			Logger.Log(Channel.PlayerStats, "PlayerStats have been saved.");
		} catch(Exception e) {
			Logger.LogError("Something went wrong while saving to the PlayerStats file.", e);
		}
	}

	public static void AddKill() {
		Instance.TotalKills++;
		SavePlayerStats();
	}

	public static void AddDeath() {
		Instance.TotalDeaths++;
		SavePlayerStats();
	}

	public static void AddTravelledDistance(int amount) {
		Instance.TotalDistanceTravelled += amount;
		SavePlayerStats();
	}

	public static void AddTotalPlaytime(int amount) {
		Instance.TotalPlaytime += amount;
		SavePlayerStats();
	}

	public static void AddScore(int amount) {
		Instance.TotalScore += amount;
		SavePlayerStats();
	}

	public static void AddLevelsCompleted() {
		Instance.TotalLevelsCompleted++;
		SavePlayerStats();
	}

	public static void AddBossesKilled() {
		Instance.TotalBossesKilled++;
		SavePlayerStats();
	}
}
