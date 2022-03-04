using System;
using System.IO;
using BayatGames.SaveGameFree;
using BayatGames.SaveGameFree.Serializers;

public class PlayerStats {

	public static PlayerStats Instance { get; set; }
	public int TotalKills { get; set; }
	public int TotalPlaytime { get; set; }
	public int TotalLevelsCompleted { get; set; }
	public int TotalDeaths { get; set; }
	public bool ArcadeCompletedOnce => DifficultyEasyCompleted || DifficultyMediumCompleted || DifficultyHardCompleted || DifficultyOriginalCompleted;
	public int TotalDistanceTravelled	{ get; set; }
	public bool DifficultyEasyCompleted { get; set; }
	public bool DifficultyMediumCompleted { get; set; }
	public bool DifficultyHardCompleted { get; set; }
	public bool DifficultyOriginalCompleted { get; set; }
	public int TotalBossesKilled { get; set; }
	public int TotalScore { get; set; }
	public DateTime LastModified { get; set; }
	public static string FilePath => GamePaths.PlayerStatsFile;
	public static string PlayerStatsVersionNumber;
	public static bool EncodeSaveGame = true;

	public static void GameStartup() {
		SaveGame.Serializer = new SaveGameJsonSerializer();
		SaveGame.LogError = true;
#if UNITY_EDITOR
		EncodeSaveGame = false;
#endif
		SaveGame.Encode = EncodeSaveGame;

		LoadPlayerStats();
	}

	public static void LoadPlayerStats() {
		// Check GameSave Version Number
		SaveGame.Encode = false;    // is not encoded
		if(SaveGame.TryLoad(nameof(PlayerStatsVersionNumber), out PlayerStatsVersionNumber) && PlayerStatsVersionNumber != "") {
			Logger.Log(Channel.SaveGame, "Detected PlayerStats version " + PlayerStatsVersionNumber);
		} else {
			Logger.Log(Channel.SaveGame, "Failed to identify PlayerStats version.");
			PlayerStatsVersionNumber = "1.0";    // Needs to be updated to newest version if updated. Setting VersionNumber to highest default
			SaveGame.Save(nameof(PlayerStatsVersionNumber), "1.0");
		}
		SaveGame.Encode = EncodeSaveGame;

		switch(PlayerStatsVersionNumber) {
			case "1.0":
				if(SaveGame.TryLoad(nameof(PlayerStats), out PlayerStats _instance)) {
					Instance = _instance;
				} else {
					Logger.Log(Channel.SaveGame, "No compatible PlayerStats have been found. Creating new one.");
					Instance = new PlayerStats();
					SavePlayerStats();
				}
				break;
			default:
				Instance = new PlayerStats();
				SavePlayerStats();
				break;
		}
	}

	public static void SavePlayerStats() {
		try {
			if(Game.IsGameRunning && Game.IsGameRunningDebug == false) {
				Instance.LastModified = DateTime.Now;
				SaveGame.Save(nameof(PlayerStats), Instance);

				Logger.Log(Channel.PlayerStats, "PlayerStats have been saved.");
			}
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
