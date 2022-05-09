using UnityEngine;

public static class GamePaths {

	// Game Paths
	public static string GameFolder = Application.persistentDataPath;
	public static string SaveGamePath = GameFolder + "/SaveGame";
	public static string UserLevelsFolder => GameFolder + "/CustomLevels";
	public static string UserGraphicSettings => GameFolder + "/graphics.ini";
	public static string ThemesPath => "Themes";
	public static string PlayerStatsFile => GameFolder + "/PlayerStats.dat";
	public static string GetLevelPath(LevelData data) => ValidateLevelPath($"{UserLevelsFolder}/{data.levelName}_{data.levelId}.json");
	public static string ValidateLevelPath(string path) => path.Trim().ToLower().Replace(" ", "");

	// Editor Paths
	public static string Official_Levels_Folder => $"{Application.dataPath}/Addressables/Levels";
	public static string GetOfficialLevelPath(LevelData levelData) => GetOfficialLevelPath(levelData.levelId);
	public static string GetOfficialLevelPath(ulong levelId) => $"{Official_Levels_Folder}/Level_{levelId}.json";
}
