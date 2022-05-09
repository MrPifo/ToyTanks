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
						editor.loadedLevelId = (ulong)args[0].Int;
						editor.SaveAsOfficialLevel();
						AssetDatabase.Refresh();
						LoadCampaignLevel(new CommandArg[1] { args[0] });
					} catch(System.Exception e) {
						Terminal.Log(TerminalLogType.Error, e.Message);
					}
				} else if(args.Length == 2) {
					try {
						editor.LoadOfficialLevel(AssetLoader.GetOfficialLevel((ulong)args[1].Int));
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
			Logger.LogError(e, "Failed to load level.");
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

	[RegisterCommand(Help = "Patch Ground Together")]
	public static void PatchGround(CommandArg[] args) {
		try {
			LevelGround.PatchTiles();
			Terminal.Log("Ground has been patched.");
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to patch ground.");
		}
	}
#endif

	[RegisterCommand(Help = "Unlock a Body/Head part.", Hint = "UnlockPart [Body/Head] [id]")]
	public static void UnlockPart(CommandArg[] args) {
		try {
			if (args != null && args.Length > 1) {
				if(args[0].String.ToLower() == "body") {
					GameSaver.UnlockPart(TankPartAsset.TankPartType.Body, args[1].Int);
				} else if(args[0].String.ToLower() == "head") {
					GameSaver.UnlockPart(TankPartAsset.TankPartType.Head, args[1].Int);
				} else {
					throw new System.Exception("TankPart " + args[0].String + " not found.");
				}
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to unlock part.");
		}
	}

	[RegisterCommand(Help = "Unlock an ability", Hint = "UnlockAbility [1]")]
	public static void UnlockAbility(CommandArg[] args) {
		try {
			if (args != null && args.Length > 0) {
				if (args[0].Int < System.Enum.GetValues(typeof(CombatAbility)).Length - 1) {
					GameSaver.UnlockAbility((CombatAbility)args[0].Int);
				}
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to unlock ability.");
		}
	}

	[RegisterCommand(Help = "Unlocks all available abilities.")]
	public static void UnlockAllAbilities(CommandArg[] args) {
		try {
			foreach (var ab in System.Enum.GetValues(typeof(CombatAbility))) {
				GameSaver.UnlockAbility((CombatAbility)ab);
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to unlock all abilities.");
		}
	}

	[RegisterCommand(Help = "Unlock all available Body/Head parts.")]
	public static void UnlockAllPart(CommandArg[] args) {
		try {
			foreach(var p in AssetLoader.GetParts(TankPartAsset.TankPartType.Body)) {
				GameSaver.UnlockPart(TankPartAsset.TankPartType.Body, p.id);
			}
			foreach (var p in AssetLoader.GetParts(TankPartAsset.TankPartType.Head)) {
				GameSaver.UnlockPart(TankPartAsset.TankPartType.Head, p.id);
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to unlock all Body/Head parts.");
		}
	}

	[RegisterCommand(Help = "Switch Game Controls", Hint = "Desktop = 0, DoubleDPad = 1, DpadAndTap = 2, DoubleDPadAimAssistant = 3")]
	public static void ChangeControls(CommandArg[] args) {
		try {
			if(args != null && args.Length > 0) {
				PlayerInputManager.SetPlayerControlScheme((PlayerControlSchemes)args[0].Int);
				Terminal.Log("Game controls have been changed to " + Game.PlayerControlScheme);
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "Failed to change game controls.");
		}
	}

	[RegisterCommand(Help = "Show Tank Debug Visual (Pathfinding, Grid, Radius etc.)")]
	public static void ShowTankDebugs(CommandArg[] args) {
		AIManager.showTankDebugs = true;
	}

	[RegisterCommand(Help = "")]
	public static void HideTankDebugs(CommandArg[] args) {
		AIManager.showTankDebugs = false;
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
		var player = Object.FindObjectOfType<PlayerTank>();
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
		var player = Object.FindObjectOfType<PlayerTank>();
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
		var player = Object.FindObjectOfType<PlayerTank>();
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
					editor.SaveAsOfficialLevel();
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

	[RegisterCommand(Help = "Changes the difficulty of tanks")]
	public static void SwitchDifficulty(CommandArg[] args) {
		try {
			if(args.Length > 0) {
				CampaignV1.Difficulty diff = CampaignV1.Difficulty.Medium;
				foreach(var e in System.Enum.GetValues(typeof(CampaignV1.Difficulty))) {
					if(args[0].String.ToLower() == e.ToString().ToLower()) {
						diff = (CampaignV1.Difficulty)e;
					}
				}
				foreach(var t in Object.FindObjectsOfType<TankBase>()) {
					t.SetDifficulty(diff);
				}
				Terminal.Log("Difficulty has been set to " + diff);
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "You need to be in a level to skip.");
		}
	}

	[RegisterCommand(Help = "Skips or Ends the current level")]
	public static void Skip(CommandArg[] args) {
		var level = Object.FindObjectOfType<LevelManager>();
		try {
			if(level != null) {
				LevelManager.Instance?.GameOver();
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "You need to be in a level to skip.");
		}
	}

	[RegisterCommand(Help = "Respawns the player")]
	public static void Respawn(CommandArg[] args) {
		var player = Object.FindObjectOfType<PlayerTank>();
		try {
			if(player != null) {
				player.ResetState();
				Object.FindObjectOfType<PlayerTank>().InitializeTank();
			} else {
				throw new System.Exception();
			}
		} catch {
			Terminal.Log(TerminalLogType.Error, "No active player found.");
		}
	}

	[RegisterCommand(Help = "Respawns all AIS")]
	public static void ResetAI(CommandArg[] args) {
		var ais = Object.FindObjectsOfType<TankAI>();
		try {
			if (ais.Length > 0) {
				foreach(var ai in ais) {
					ai.ResetState();
					(ai.TempNewTank as TankAI).EnableAI();
					ai.TempNewTank.InitializeTank();
				}
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
		var player = Object.FindObjectOfType<PlayerTank>();
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
		var player = Object.FindObjectOfType<PlayerTank>();
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
