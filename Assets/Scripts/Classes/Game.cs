using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

// This class holds all the Games information
// - Existing Worlds
// - Existing Levels
// - Avialable Achievements
public static class Game {

	static readonly List<World> Worlds = new List<World>() {
		new World(global::Worlds.WoodWorld, new Level[10] {
			new Level(0, 1, false),
			new Level(1, 2, false),
			new Level(2, 3, false),
			new Level(3, 4, false),
			new Level(4, 5, false),
			new Level(5, 6, false),
			new Level(6, 7, false),
			new Level(7, 8, false),
			new Level(8, 9, false),
			new Level(9, 10, true),
		}, new MenuCameraSettings() {
			orthograpicSize = 28,
			pos = new Vector3(80, 25, -14),
			rot = new Vector3(60, 0, 0),
		}),

		new World(global::Worlds.FloorWorld, new Level[10] {
			new Level(0, 11, false),
			new Level(1, 12, false),
			new Level(2, 13, false),
			new Level(3, 14, false),
			new Level(4, 15, false),
			new Level(5, 16, false),
			new Level(6, 17, false),
			new Level(7, 18, false),
			new Level(8, 19, false),
			new Level(9, 20, true),
		}, new MenuCameraSettings() {
			orthograpicSize = 28,
			pos = new Vector3(80, -75, -14),
			rot = new Vector3(60, 0, 0),
		})
	};
	public static Dictionary<string, Texture2D> Cursors { get; set; } = new Dictionary<string, Texture2D>();
	public static string LevelScreenshotPath => "Levels/Screenshots/Level_";

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
		public Level(int order, int levelId, bool isBoss) {
			this.order = order;
			this.levelId = levelId;
			this.isBoss = isBoss;
		}
		readonly int levelId;
		readonly int order;
		readonly bool isBoss;

		public int Order => order;
		public int LevelId => levelId;
		public bool IsBoss => isBoss;
	}

	// Getter Methods
	public static World[] GetWorlds => Worlds.ToArray();
	public static World GetWorld(Worlds worldType) => Worlds.Find(w => w.WorldType == worldType);
	public static World GetWorld(int levelId) => Worlds.Find(w => w.Levels.Any(l => l.LevelId == levelId));
	public static Level[] GetLevels(Worlds worldType) => GetWorld(worldType).Levels;
	public static Level GetLevelByOrder(Worlds worldType, int order) => GetWorld(worldType).Levels.Where(l => l.Order == order).First();
	public static Level GetLevelById(Worlds worldType, int levelId) => GetWorld(worldType).Levels.Where(l => l.LevelId == levelId).First();
	public static int LevelCount(Worlds worldType) => GetWorld(worldType).Levels.Length;
	public static void SetCursor(string cursor = "") {
		if(Cursors.ContainsKey(cursor.ToLower())) {
			try {
				Cursor.SetCursor(Cursors[cursor.ToLower()], Vector2.zero, CursorMode.ForceSoftware);
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
