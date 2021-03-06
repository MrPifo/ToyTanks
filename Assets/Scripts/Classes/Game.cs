using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using CommandTerminal;
using System.IO;
using System.Text;
using System.IO.Compression;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

// This class holds all the Games information
// - Existing Worlds
// - Existing Levels
// - Avialable Achievements
public static class Game {

	static readonly List<World> Worlds = new List<World>() {
		new World(global::WorldTheme.Woody, new Level[10] {
			new Level(0, 1),
			new Level(1, 2),
			new Level(2, 3),
			new Level(3, 4),
			new Level(4, 5),
			new Level(5, 6),
			new Level(6, 7),
			new Level(7, 8),
			new Level(8, 9),
			new Level(9, 10, true),
		}, new MenuCameraSettings() {
			orthograpicSize = 24,
			pos = new Vector3(80, 25, -14),
			rot = new Vector3(60, 0, 0),
		}),

		new World(global::WorldTheme.Fir, new Level[10] {
			new Level(0, 11),
			new Level(1, 12),
			new Level(2, 13),
			new Level(3, 14),
			new Level(4, 15),
			new Level(5, 16),
			new Level(6, 17),
			new Level(7, 18),
			new Level(8, 19),
			new Level(9, 20, true),
		}, new MenuCameraSettings() {
			orthograpicSize = 24,
			pos = new Vector3(80, -75, -14),
			rot = new Vector3(60, 0, 0),
		}),
		new World(global::WorldTheme.Snowy, new Level[10] {
			new Level(0, 21),
			new Level(1, 22),
			new Level(2, 23),
			new Level(3, 24),
			new Level(4, 25),
			new Level(5, 26),
			new Level(6, 27),
			new Level(7, 28),
			new Level(8, 29),
			new Level(9, 30),
		}, new MenuCameraSettings() {
			orthograpicSize = 24,
			pos = new Vector3(80, -175, -14),
			rot = new Vector3(60, 0, 0),
		}),
		new World(global::WorldTheme.Garden, new Level[10] {
			new Level(0, 31),
			new Level(0, 32),
			new Level(0, 33),
			new Level(0, 34),
			new Level(0, 35),
			new Level(0, 36),
			new Level(0, 37),
			new Level(0, 38),
			new Level(0, 39),
			new Level(0, 40),
		}, new MenuCameraSettings() {
			orthograpicSize = 24,
			pos = new Vector3(80, -275, -14),
			rot = new Vector3(60, 0, 0),
		}),
	};
	//public static Dictionary<string, Texture2D> Cursors { get; set; } = new Dictionary<string, Texture2D>();
	public static string LevelScreenshotPath => "Levels/Screenshots/Level_";
	public static Level[] Levels => Worlds.SelectMany(l => l.Levels).ToArray();
	public static ulong TotalLevels => (ulong)Levels.Length;
	
	public static bool IsTerminal => Terminal.Instance == null ? false : !Terminal.Instance.IsClosed;
	public static bool isPlayerGodMode;
	/// <summary>
	/// Fixed Timesteps that have been passed since playing.
	/// </summary>
	public static ulong FixedSteps => Instance.fixedSteps;
	public static bool ApplicationInitialized { get; set; }
	/// <summary>
	/// True when hit pause within a level.
	/// </summary>
	public static bool GamePaused { get; set; }
	/// <summary>
	/// True when a level has been started.
	/// </summary>
	public static bool IsGameRunning { get; set; }
	/// <summary>
	/// True when a level has been started manually within the Editor.
	/// </summary>
	public static bool IsGameRunningDebug { get; set; }
	/// <summary>
	/// True when a level has been started and the player is allowed to move.
	/// </summary>
	public static bool IsGamePlaying { get; set; }
	/// <summary>
	/// True if are IsGamePlaying && GamePaused == false && IsTerminal == false;
	/// Generally speaking if the player is able to move and shoot without the game being paused or whatsoever.
	/// </summary>
	public static bool IsGameCurrentlyPlaying => IsGamePlaying && GamePaused == false && IsTerminal == false;
	/// <summary>
	/// Keeps track of overall info of the player. Useful for Achievments later on. 
	/// </summary>
	public static PlayerStats PlayerStats { get; set; }

	public static GamePlatform Platform { get; set; }
	public static PlayerControlSchemes PlayerControlScheme { get; private set; }
	private static GameMono _instance;
	public static GameMono Instance {
		get {
			if(_instance == null) {
				_instance = new GameObject("").AddComponent<GameMono>();
				UnityEngine.Object.DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}

	public class World {
		public World(WorldTheme worldType, Level[] levels, MenuCameraSettings menuCameraSettings) {
			this.levels = levels;
			this.worldType = worldType;
			this.menuCameraSettings = menuCameraSettings;
		}
		readonly WorldTheme worldType;
		readonly Level[] levels;
		readonly MenuCameraSettings menuCameraSettings;

		public WorldTheme WorldType => worldType;
		public Level[] Levels => levels;
		public MenuCameraSettings MenuCameraSettings => menuCameraSettings;

		public Level GetLevel(int id) => new List<Level>(levels).Find(l => l.Order == id);
	}
	public class Level {
		public Level(int order, ulong levelId, bool isBoss = false) {
			this.order = order;
			this.levelId = levelId;
			this.isBoss = isBoss;
		}
		readonly ulong levelId;
		readonly int order;
		readonly bool isBoss;

		public int Order => order;
		public ulong LevelId => levelId;
		public bool IsBoss => isBoss;
	}

	/// <summary>
	/// Must be called whenever the game is started. Returns an error string if something went wrong.
	/// </summary>
	public static async UniTask Initialize() {
		if(ApplicationInitialized == false) {
			Logger.ClearLogFile();
			Unity.MLAgents.Academy.Instance.AutomaticSteppingEnabled = false;
			await GameStartup.SetLoadingText("Initializing Assets");
			await Addressables.InitializeAsync();
			CheckPlatform();

			// Graphic Settings
			try {
				await GameStartup.SetLoadingText("Loading Graphic-Settings");
				GraphicSettings.Initialize();
			} catch(Exception e) {
				Logger.LogError(e, "Failed to initialize GraphicSettings.");
			}

			// Input Manager
			try {
				await GameStartup.SetLoadingText("Initializing Input-System");
				PlayerInputManager.SetPlayerControlScheme(PlayerControlScheme);
			} catch(Exception e) {
				Logger.LogError(e, "Failed to initialize InputManager.");
			}


			// SaveGame
			try {
				await GameStartup.SetLoadingText("Loading Progression");
				GameSaver.GameStartUp();
			} catch(Exception e) {
				Logger.LogError(e, "Critical error occurred while loading the games SaveGame file. Progress will be reset.");
			}

			// Runtime Analytics, only run this when build
			try {
				await GameStartup.SetLoadingText("Connecting to Game-Analytics");
				await RuntimeAnalytics.Initialize();
			} catch (Exception e) {
				Logger.LogError(e, "Failed to initialize GameAnalytics.");
			}

			await UniTask.Delay(50);
			// Player Stats
			try {
				//await GameStartup.SetLoadingText("Loading Achievements");
				//PlayerStats.GameStartup();
			} catch(Exception e) {
				Logger.LogError(e, "Critical error occured while loading the Player Stats. Progress will be reset.");
			}

			PlayerInputManager.HideControls();
			// Refresh the games save files timestamps
			GameSaver.Save();

			await GameStartup.SetLoadingText("Loading Game Assets");
			await AssetLoader.PreloadAssetsStartup();
			await GameStartup.SetLoadingText("");
			// End of loading
			ApplicationInitialized = true;
        }
    }
	public static void CheckPlatform() {
#if UNITY_ANDROID
		Platform = GamePlatform.Mobile;
#endif
#if UNITY_STANDALONE
		Platform = GamePlatform.Desktop;
#endif
		Logger.Log(Channel.Platform, "Game has been started in " + Platform + " mode.");
	}
	public static bool EveryFixedFrame(int step) => FixedSteps % (ulong)step == 0;

	/// <summary>
	/// Change the player input control scheme. Only available for mobile platform.
	/// </summary>
	public static void ChangeControls(PlayerControlSchemes controlScheme) {
		if(Platform == GamePlatform.Mobile) {
			PlayerControlScheme = controlScheme;
			Logger.Log(Channel.Gameplay, "Player control scheme has been switched to " + controlScheme.ToString());
		} else {
			PlayerControlScheme = PlayerControlSchemes.Desktop;
		}
    }

	// Getter Methods
	public static World[] GetWorlds => Worlds.ToArray();
	public static World GetWorld(WorldTheme worldType) => Worlds.Find(w => w.WorldType == worldType);
	public static World GetWorld(ulong levelId) => Worlds.Find(w => w.Levels.Any(l => l.LevelId == levelId));
	public static Level[] GetLevels(WorldTheme worldType) => GetWorld(worldType).Levels;
	public static Level GetLevelByOrder(WorldTheme worldType, int order) => GetWorld(worldType).Levels.Where(l => l.Order == order).First();
	public static Level GetLevelById(ulong levelId) => Levels.Where(l => l.LevelId == levelId).First();
	public static bool LevelExists(ulong levelId) => Levels.Any(l => l.LevelId == levelId);
	public static int LevelCount(WorldTheme worldType) => GetWorld(worldType).Levels.Length;
	public static void SetCursor(string cursor = "") {
		/*if(Cursors.ContainsKey(cursor.ToLower())) {
			try {
				Cursor.SetCursor(Cursors[cursor.ToLower()], Vector2.zero, CursorMode.Auto);
			} catch {
				Logger.Log(Channel.Rendering, "Failed setting Cursor Texture!");
			}
		} else {
			try {
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			} catch {
				Logger.Log(Channel.Rendering, "Failed setting Cursor Texture to null");
			}
		}*/
	}
	public static void AddCursor(string name, Texture2D texture) {
		/*if(!Cursors.ContainsKey(name)) {
			Cursors.Add(name, texture);
		}*/
	}

	// File Helper Methods
	/// <summary>
	/// Compresses and returns the given string.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static string CompressString(string str) {
		var bytes = Encoding.UTF8.GetBytes(str);

		using(var msi = new MemoryStream(bytes))
		using(var mso = new MemoryStream()) {
			using(var gs = new GZipStream(mso, CompressionMode.Compress)) {
				//msi.CopyTo(gs);
				CopyTo(msi, gs);
			}

			return Convert.ToBase64String(mso.ToArray());
		}
	}
	/// <summary>
	/// Decompresses and returns the given string.
	/// </summary>
	/// <param name="compressedString"></param>
	/// <returns></returns>
	public static string DecompressString(string compressedString) {
		try {
			byte[] bytes = Convert.FromBase64String(compressedString);
			using (var msi = new MemoryStream(bytes))
			using (var mso = new MemoryStream()) {
				using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
					//gs.CopyTo(mso);
					CopyTo(gs, mso);
				}

				return Encoding.UTF8.GetString(mso.ToArray());
			}
		} catch {
			Debug.LogWarning("Failed to decompress string");
			return compressedString;
        }
	}
	private static void CopyTo(Stream src, Stream dest) {
		byte[] bytes = new byte[4096];

		int cnt;

		while((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
			dest.Write(bytes, 0, cnt);
		}
	}
	/// <summary>
	/// Writes the given string to a file.
	/// </summary>
	/// <param name="content"></param>
	/// <param name="compress"></param>
	public static void WriteToFile(string content, string path, bool compress = false) {
		try {
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Write);
			using var sw = new StreamWriter(stream);
			sw.BaseStream.SetLength(0);

			if(compress) {
				content = CompressString(content);
			}
			
			sw.Write(content);
			sw.Close();
		} catch(FileNotFoundException e) {
			Logger.LogError(e, "Failed to write content to " + path);
			throw e;
		} catch(IOException e) {
			Logger.LogError(e, "Something else went wrong writing content to file " + path);
			throw e;
		}
	}
	public static string ReadFromFile(string path, bool decompress = false) {
		try {
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
			using var sw = new StreamReader(stream);
			string content = sw.ReadToEnd();
			sw.Close();

			if(decompress) {
				content = DecompressString(content);
			}

			return content;
		} catch(FileNotFoundException e) {
			Logger.LogError(e, "Failed to read content from " + path);
			throw e;
		} catch(IOException e) {
			Logger.LogError(e, "Something else went wrong reading content from file " + path);
			throw e;
		}
	}
	public static bool CreateFile(string path) {
		if(File.Exists(path) == false) {
			using var fs = File.Create(path);
			fs.Close();
			return true;
		} else {
			Logger.Log(Channel.SaveGame, "File already exists on " + path);
			return false;
		}
	}
	public static bool DeleteFile(string path) {
		if(File.Exists(path)) {
			try {
				File.Delete(path);
				return true;
			} catch(Exception e) {
				Logger.LogError(e, "Failed to delete file " + path);
				return false;
			}
		}
		return false;
	}
	public static void RenameFile(string path, string newName) {
		FileInfo info = new FileInfo(path);
		if(info.Exists) {
			path = path.Replace(Path.GetFileNameWithoutExtension(path), newName);
			info.MoveTo(path);
		}
	}
	public static void CreateBackupOfFile(string path) {
		if(File.Exists(path)) {
			string filename = Path.GetFileNameWithoutExtension(path);
			string backupPath = path.Replace(filename, filename + "_corrutped_backup_" + DateTime.Now.ToFileTime());
			File.Copy(path, backupPath);
		}
	}



	public class GameMono : MonoBehaviour {

		public ulong fixedSteps;

		private void FixedUpdate() {
			fixedSteps++;
			if(fixedSteps < 0) {
				fixedSteps = 0;
			}
		}

	}
}
