using System;
using ToyTanks.LevelEditor;
using UnityEngine.AddressableAssets;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class AssetLoader {

	private static ThemeAsset[] _bufferedThemes;
	public static ThemeAsset[] ThemeAssets {
		get {
			if(_bufferedThemes == null || _bufferedThemes.Length == 0) {
				var waiter = Addressables.LoadAssetsAsync<ThemeAsset>("Themes", null);
				waiter.WaitForCompletion();
				_bufferedThemes = new ThemeAsset[waiter.Result.Count];

				for(int i = 0; i < _bufferedThemes.Length; i++) {
					_bufferedThemes[i] = waiter.Result[i];
				}
				_bufferedThemes = _bufferedThemes.OrderBy(t => (int)t.theme).ToArray();
			}
			return _bufferedThemes;
		}
	}
	private static TankAsset[] _bufferedTanks;
	public static TankAsset[] TankAssets {
		get {
			if(_bufferedTanks == null || _bufferedTanks.Length == 0) {
				var waiter = Addressables.LoadAssetsAsync<TankAsset>("Tanks", null);
				waiter.WaitForCompletion();
				_bufferedTanks = new TankAsset[waiter.Result.Count];

				for(int i = 0; i < _bufferedTanks.Length; i++) {
					_bufferedTanks[i] = waiter.Result[i];
				}
				_bufferedTanks = _bufferedTanks.OrderBy(t => (int)t.tankType).ToArray();
			}
			return _bufferedTanks;
		}
	}
	private static LevelData[] _bufferedLevels;
	public static LevelData[] LevelAssets {
		get {
			if(_bufferedLevels == null || _bufferedLevels.Length == 0) {
				var waiter = Addressables.LoadAssetsAsync<TextAsset>("Levels", null);
				waiter.WaitForCompletion();
				_bufferedLevels = new LevelData[waiter.Result.Count];

				for(int i = 0; i < _bufferedLevels.Length; i++) {
					_bufferedLevels[i] = JsonConvert.DeserializeObject<LevelData>(waiter.Result[i].text, new JsonSerializerSettings() {
						Formatting = Formatting.Indented,
						TypeNameHandling = TypeNameHandling.Auto,
					});
				}
				_bufferedLevels = _bufferedLevels.OrderBy(t => (int)t.levelId).ToArray();
			}
			return _bufferedLevels;
		}
		set => _bufferedLevels = value;
	}
	private static GroundTileData[] _bufferedGroundTiles;
	public static GroundTileData[] GroundTileAssets {
		get {
			if(_bufferedGroundTiles == null || _bufferedGroundTiles.Length == 0) {
				var waiter = Addressables.LoadAssetsAsync<GroundTileData>("GroundTiles", null);
				waiter.WaitForCompletion();
				_bufferedGroundTiles = new GroundTileData[waiter.Result.Count];

				for(int i = 0; i < _bufferedGroundTiles.Length; i++) {
					_bufferedGroundTiles[i] = waiter.Result[i];
				}
				_bufferedGroundTiles = _bufferedGroundTiles.OrderBy(t => (int)t.type).ToArray();
			}
			return _bufferedGroundTiles;
		}
	}
	private static Dictionary<WorldTheme, List<BlockAsset>> _bufferedBlockAssets;
	public static Dictionary<WorldTheme, List<BlockAsset>> BlockAssets {
		get {
			if(_bufferedBlockAssets == null || _bufferedBlockAssets.Count == 0) {
				_bufferedBlockAssets = new Dictionary<WorldTheme, List<BlockAsset>>();

				foreach(var thema in Enum.GetValues(typeof(WorldTheme))) {
					var thWaiter = Addressables.LoadAssetsAsync<BlockAsset>(thema.ToString() + "Assets", null);
					thWaiter.WaitForCompletion();
					var list = new List<BlockAsset>();

					for(int i = 0; i < thWaiter.Result.Count; i++) {
						list.Add(thWaiter.Result[i]);
					}
					list = list.OrderBy(a => (int)a.block).ToList();
					_bufferedBlockAssets.Add((WorldTheme)Enum.Parse(typeof(WorldTheme), thema.ToString()), list);
				}
			}
			return _bufferedBlockAssets;
		}
	}
	private static Sprite[] _bufferedLevelPreviews;
	public static Sprite[] LevelPreviews {
		get {
			if(_bufferedLevelPreviews == null || _bufferedLevelPreviews.Length == 0) {
				var waiter = Addressables.LoadAssetsAsync<Sprite>("Assets/Addressables/Levels/Screenshots", null);
				waiter.WaitForCompletion();
				_bufferedLevelPreviews = new Sprite[waiter.Result.Count];

				for(int i = 0; i < _bufferedLevelPreviews.Length; i++) {
					_bufferedLevelPreviews[i] = waiter.Result[i];
				}
			}
			return _bufferedLevelPreviews;
		}
	}
	private static ExtraBlockAsset[] _bufferedExtraBlocks;
	public static ExtraBlockAsset[] ExtraBlocks {
		get {
			if(_bufferedExtraBlocks == null || _bufferedExtraBlocks.Length == 0) {
				var waiter = Addressables.LoadAssetsAsync<ExtraBlockAsset>("ExtraBlocks", null);
				waiter.WaitForCompletion();
				_bufferedExtraBlocks = new ExtraBlockAsset[waiter.Result.Count];

				for(int i = 0; i < _bufferedExtraBlocks.Length; i++) {
					_bufferedExtraBlocks[i] = waiter.Result[i];
				}
				_bufferedExtraBlocks = _bufferedExtraBlocks.OrderBy(t => (int)t.block).ToArray();
			}
			return _bufferedExtraBlocks;
		}
	}
	public static void PreloadAssets() {
		Logger.Log(Channel.System, "Loaded Themes: " + ThemeAssets.Length);
		Logger.Log(Channel.System, "Loaded Tanks: " + TankAssets.Length);
		Logger.Log(Channel.System, "Loaded Levels: " + LevelAssets.Length);
		Logger.Log(Channel.System, "Loaded GroundTiles: " + GroundTileAssets.Length);
		Logger.Log(Channel.System, "Loaded Blocks: " + BlockAssets.Count);
		Logger.Log(Channel.System, "Loaded LevelPreviews: " + LevelPreviews.Length);
		Logger.Log(Channel.System, "Loaded ExtraBlocks: " + ExtraBlocks.Length);
	}

	#region Getters
	public static ThemeAsset GetTheme(WorldTheme theme) => ThemeAssets.ToList().Find(t => t.theme == theme);
	public static BlockAsset GetBlockAsset(WorldTheme theme, BlockType type) => BlockAssets[theme].Find(a => a.block == type);
	public static List<BlockAsset> GetBlockAssets(WorldTheme theme) => BlockAssets[theme];
	public static TankAsset GetTank(TankTypes type) => TankAssets.ToList().Find(t => t.tankType == type);
	public static LevelData GetOfficialLevel(ulong level) => LevelAssets.ToList().Find(l => l.levelId == level);
	public static GroundTileData GetGroundTile(GroundTileType type) => GroundTileAssets.ToList().Find(t => t.type == type);
	public static Sprite GetOfficialLevelPreview(ulong level) => _bufferedLevelPreviews[(int)level];
	public static ExtraBlockAsset[] GetExtraBlockAssets() => ExtraBlocks;
	public static ExtraBlockAsset GetExtraBlockAsset(ExtraBlocks block) => GetExtraBlockAssets().ToList().Find(t => t.block == block);
	#endregion
}
