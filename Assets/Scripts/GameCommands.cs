using CommandTerminal;
using ToyTanks.LevelEditor;
using UnityEngine;
using CameraShake;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GameCommands {

#if UNITY_EDITOR
	[RegisterCommand(Help = "Creates campaign level [number] [template]")]
	public static void CreateCampaignLevel(CommandArg[] args) {
		var editor = Object.FindObjectOfType<LevelEditor>();
		try {
			if(editor != null) {
				if(args.Length == 1) {
					try {
						editor.LevelData = new LevelData() {
							levelId = (ulong)args[0].Int,
							levelName = "",
							blocks = new List<LevelData.BlockData>(),
							gridSize = GridSizes.Size_12x9,
							tanks = new List<LevelData.TankData>(),
							theme = WorldTheme.Woody,
							groundTiles = new List<LevelData.GroundTileData>()
						};
						AssetLoader.LevelAssets = null;
						editor.loadedLevelId = (ulong)args[0].Int;
						editor.SaveAsOfficialLevel();
						AssetDatabase.Refresh();
						LoadCampaignLevel(new CommandArg[1] { args[0] });
					} catch(System.Exception e) {
						Terminal.Log(TerminalLogType.Error, e.Message);
					}
				} else if(args.Length == 2) {
					try {
						editor.LoadOfficialLevel((ulong)args[1].Int);
						editor.LevelData = new LevelData() {
							levelId = (ulong)args[0].Int,
							levelName = "",
							blocks = new System.Collections.Generic.List<LevelData.BlockData>(),
							gridSize = editor.LevelData.gridSize,
							tanks = new System.Collections.Generic.List<LevelData.TankData>(),
							theme = editor.LevelData.theme
						};
						editor.SaveAsOfficialLevel();
						AssetDatabase.Refresh();
						LoadCampaignLevel(new CommandArg[1] { args[0] });
					} catch(System.Exception e) {
						Terminal.Log(TerminalLogType.Error, e.Message);
					}
				}
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to create level.");
		}
	}

	[RegisterCommand(Help = "Loads campaign level")]
	public static void LoadCampaignLevel(CommandArg[] args) {
		var editor = Object.FindObjectOfType<LevelEditor>();
		try {
			if(editor != null) {
				editor.LoadOfficialLevel((ulong)args[0].Int);
				if(editor.LevelData != null) {
					Terminal.Log("Level has been loaded.");
				} else {
					Terminal.Log("Level not found.");
				}
			} else {
				throw new System.Exception();
			}
		} catch (System.Exception e) {
			Terminal.Log(TerminalLogType.Error, "Failed to load level.");
			Terminal.Log(TerminalLogType.Error, e.StackTrace);
		}
	}

	[RegisterCommand(Help = "Delete campaign level")]
	public static void DeleteCampaignLevel(CommandArg[] args) {
		var editor = Object.FindObjectOfType<LevelEditor>();
		try {
			if(editor != null) {
				var path = Application.dataPath + "/Resources/Levels/Level_" + args[0].Int + ".json";
				if(System.IO.File.Exists(path)) {
					System.IO.File.Delete(path);
					Terminal.Log("Level " + args[0].Int + " has been deleted.");
				} else {
					Terminal.Log("Level does not exist.");
				}
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to delete level.");
		}
	}
#endif

	[RegisterCommand(Help = "Show Tank Debug Visual (Pathfinding, Grid, Radius etc.)")]
	public static void ShowTankDebugs(CommandArg[] args) {
		foreach(var tank in Object.FindObjectsOfType<TankAI>()) {
			tank.showDebug = true;
		}
		Game.showTankDebugs = true;
	}

	[RegisterCommand(Help = "")]
	public static void HideTankDebugs(CommandArg[] args) {
		foreach(var tank in Object.FindObjectsOfType<TankAI>()) {
			tank.showDebug = false;
		}
		Game.showTankDebugs = false;
	}

	[RegisterCommand(Help = "Clears the save file. Carefully to use!!!")]
	public static void ClearSaveFile(CommandArg[] args) {
		try {
			System.IO.File.Copy(GamePaths.SaveGamePath, GamePaths.GameFolder + "/SaveGame_backup.dat");
			System.IO.File.Delete(GamePaths.SaveGamePath);
			GameSaver.GameStartUp();
			Terminal.Log("Save file has been deleted. A backup has been created at: " + GamePaths.GameFolder + "/SaveGame_backup.dat");
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed restoring default settings.");
		}
	}

	[RegisterCommand(Help = "Restore Graphics to Default")]
	public static void ResetGraphics(CommandArg[] args) {
		try {
			GraphicSettings.ResetSettings();
			Terminal.Log("Graphics have been restored to default.");
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed restoring default settings.");
		}
	}

	[RegisterCommand(Help = "Turn On/Off invincible mode")]
	public static void God(CommandArg[] args) {
		var player = Object.FindObjectOfType<PlayerInput>();
		try {
			if(player != null) {
				if(player.makeInvincible) {
					player.makeInvincible = false;
					Game.isPlayerGodMode = false;
					Terminal.Log("You are now not more invincible.");
				} else {
					player.makeInvincible = true;
					Game.isPlayerGodMode = true;
					Terminal.Log("You are now invincible");
				}
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "No active player found.");
		}
	}

	[RegisterCommand(Help = "Kill yourself")]
	public static void Kill(CommandArg[] args) {
		var player = Object.FindObjectOfType<PlayerInput>();
		try {
			if(player != null) {
				player.TakeDamage(null, true);
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "No active player found.");
		}
	}

	[RegisterCommand(Help = "Toggle if player can be controlled")]
	public static void ForceControls(CommandArg[] args) {
		var player = Object.FindObjectOfType<PlayerInput>();
		try {
			if(player != null) {
				if(player.IgnoreDisables) {
					player.IgnoreDisables = false;
					Terminal.Log("Player now behaves normally.");
				} else {
					player.IgnoreDisables = true;
					Terminal.Log("Player is now always controllable.");
				}
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "No active player found.");
		}
	}

#if UNITY_EDITOR
	[RegisterCommand(Help = "Save the current level in Editor mode")]
	public static void SaveLevel(CommandArg[] args) {
		var editor = Object.FindObjectOfType<LevelEditor>();
		try {
			if(editor != null && editor.LevelData != null) {
				if(Game.IsGameRunningDebug) {
					editor.SaveAsOfficialLevel();
					Terminal.Log("Campaign level has been saved.");
				} else {
					editor.SaveCustomLevel();
					Terminal.Log("Level has been saved.");
				}
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error,"Failed to save level.");
		}
	}
#endif

	[RegisterCommand(Help = "Skips or Ends the current level")]
	public static void Skip(CommandArg[] args) {
		var level = Object.FindObjectOfType<LevelManager>();
		try {
			if(level != null) {
				Terminal.Instance.StartCoroutine(level.GameOver());
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "You need to be in a level to skip.");
		}
	}

	[RegisterCommand(Help = "Respawns the player")]
	public static void Respawn(CommandArg[] args) {
		var player = Object.FindObjectOfType<PlayerInput>();
		try {
			if(player != null) {
				player.ResetState();
				Object.FindObjectOfType<PlayerInput>().InitializeTank();
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "No active player found.");
		}
	}

	[RegisterCommand(Help = "Pauses the game")]
	public static void Pause(CommandArg[] args) {
		Game.GamePaused = true;
		var player = Object.FindObjectOfType<PlayerInput>();
		try {
			if(player != null) {
				player.DisableControls();
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "No active player found.");
		}
	}

	[RegisterCommand(Help = "Resumes the game")]
	public static void Resume(CommandArg[] args) {
		Game.GamePaused = false;
		var player = Object.FindObjectOfType<PlayerInput>();
		try {
			if(player != null) {
				player.EnableControls();
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "No active player found.");
		}
	}

	[RegisterCommand(Help = "Add Lives - Max 255")]
	public static void AddLives(CommandArg[] args) {
		try {
			if(args[0].Int < 0 || args[0].Int > 255) {
				throw new System.Exception();
			}
			GameManager.PlayerLives += (byte)args[0].Int;
			Terminal.Log("New Live balance: " + GameManager.PlayerLives);
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed adding lives. Range must be within 0 - 255");
		}
	}

	[RegisterCommand(Help = "Remove Lives")]
	public static void RemoveLives(CommandArg[] args) {
		try {
			if(args[0].Int < 0 || args[0].Int > 255) {
				throw new System.Exception();
			}
			GameManager.PlayerLives -= (byte)args[0].Int;
			Terminal.Log("New Live balance: " + GameManager.PlayerLives);
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed removing lives. Range must be within 0 - 255");
		}
	}

	[RegisterCommand(Help = "Disable AI")]
	public static void DisableAI(CommandArg[] args) {
		try {
			var ais = Object.FindObjectsOfType<TankAI>();
			foreach(var ai in ais) {
				ai.DisableAI();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to disable AI.");
		}
	}

	[RegisterCommand(Help = "Enable AI")]
	public static void EnableAI(CommandArg[] args) {
		try {
			var ais = Object.FindObjectsOfType<TankAI>();
			foreach(var ai in ais) {
				ai.EnableAI();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to enable AI.");
		}
	}

	[RegisterCommand(Help = "Shake Camera")]
	public static void Shake(CommandArg[] args) {
		try {
			CameraShaker.Presets.Explosion2D();
		} catch {
			Terminal.Log(TerminalLogType.Error, "There is no active Camera Shaker.");
		}
	}

	[RegisterCommand(Help = "")]
	public static void UnlockLevel(CommandArg[] args) {
		try {
			if(args.Length == 1) {
				GameSaver.UnlockLevel((ulong)args[0].Int);
			} else if(args.Length == 2) {
				for(int i = args[0].Int; i <= args[1].Int; i++) {
					GameSaver.UnlockLevel((ulong)i);
				}
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to unlock level.");
		}
	}
}
