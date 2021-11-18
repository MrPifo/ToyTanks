using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using CommandTerminal;

// This class holds all the Games information
// - Existing Worlds
// - Existing Levels
// - Avialable Achievements
public static class Game {

	static readonly List<World> Worlds = new List<World>() {
		new World(global::Worlds.WoodWorld, new Level[10] {
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
			orthograpicSize = 22,
			pos = new Vector3(80, 25, -14),
			rot = new Vector3(60, 0, 0),
		}),

		new World(global::Worlds.FloorWorld, new Level[10] {
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
			orthograpicSize = 22,
			pos = new Vector3(80, -75, -14),
			rot = new Vector3(60, 0, 0),
		}),

		new World(global::Worlds.Basement, new Level[10] {
			new Level(0, 21),
			new Level(1, 22),
			new Level(2, 23),
			new Level(3, 24),
			new Level(4, 25),
			new Level(5, 26),
			new Level(6, 27),
			new Level(7, 28),
			new Level(8, 29),
			new Level(9, 30, true),
		}, new MenuCameraSettings() {
			orthograpicSize = 22,
			pos = new Vector3(80, -175, -14),
			rot = new Vector3(60, 0, 0),
		}),
	};
	public static Dictionary<string, Texture2D> Cursors { get; set; } = new Dictionary<string, Texture2D>();
	public static string LevelScreenshotPath => "Levels/Screenshots/Level_";
	public static Level[] Levels => Worlds.SelectMany(l => l.Levels).ToArray();
	public static ulong TotalLevels => (ulong)Levels.Length;
	public static AIGrid ActiveGrid { get; set; }
	public static bool IsTerminal => Terminal.Instance == null ? false : !Terminal.Instance.IsClosed;
	public static bool showGrid;
	public static bool showTankDebugs;
	public static bool isPlayerGodMode;

	public class World {
		public World(Worlds worldType, Level[] levels, MenuCameraSettings menuCameraSettings) {
			this.levels = levels;
			this.worldType = worldType;
			this.menuCameraSettings = menuCameraSettings;
		}
		readonly Worlds worldType;
		readonly Level[] levels;
		readonly MenuCameraSettings menuCameraSettings;

		public Worlds WorldType => worldType;
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

	// Generation Methods
	public static void CreateAIGrid(GridSizes size, LayerMask mask, bool visualize = false) {
		var existingGrid = UnityEngine.Object.FindObjectOfType<AIGrid>();
		if(existingGrid != null) {
			UnityEngine.Object.Destroy(existingGrid.gameObject);
		}
		ActiveGrid = new GameObject().AddComponent<AIGrid>();
		ActiveGrid.GenerateGrid(size, mask);
		ActiveGrid.name = "AIGrid";
	}

	// Getter Methods
	public static World[] GetWorlds => Worlds.ToArray();
	public static World GetWorld(Worlds worldType) => Worlds.Find(w => w.WorldType == worldType);
	public static World GetWorld(ulong levelId) => Worlds.Find(w => w.Levels.Any(l => l.LevelId == levelId));
	public static Level[] GetLevels(Worlds worldType) => GetWorld(worldType).Levels;
	public static Level GetLevelByOrder(Worlds worldType, int order) => GetWorld(worldType).Levels.Where(l => l.Order == order).First();
	public static Level GetLevelById(Worlds worldType, ulong levelId) => GetWorld(worldType).Levels.Where(l => l.LevelId == levelId).First();
	public static bool LevelExists(ulong levelId) => Levels.Any(l => l.LevelId == levelId);
	public static int LevelCount(Worlds worldType) => GetWorld(worldType).Levels.Length;
	public static void SetCursor(string cursor = "") {
		if(Cursors.ContainsKey(cursor.ToLower())) {
			try {
				Cursor.SetCursor(Cursors[cursor.ToLower()], Vector2.zero, CursorMode.Auto);
			} catch {
				Debug.LogError("Failed setting Cursor Texture!");
			}
		} else {
			try {
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			} catch {
				Debug.LogError("Failed setting Cursor Texture to null");
			}
		}
	}
	public static void AddCursor(string name, Texture2D texture) {
		if(!Cursors.ContainsKey(name)) {
			Cursors.Add(name, texture);
		}
	}
}
