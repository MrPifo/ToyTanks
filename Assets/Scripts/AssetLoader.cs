using System;
using ToyTanks.LevelEditor;
using UnityEngine.AddressableAssets;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

public class AssetLoader {

	public static PhysicMaterial BounceFriction;
	public static PhysicMaterial NoFriction;
	public static PhysicMaterial DefaultFriction;
	public static Sprite[] LevelPreviews { get; private set; }
	public static ThemeAsset[] ThemeAssets { get; private set; }
	public static TankAsset[] TankAssets { get; private set; }
	public static LevelData[] LevelAssets { get; private set; }
	public static FloraAsset[] FloraBlocks { get; private set; }
	public static GroundTileData[] GroundTiles { get; private set; }
	public static ExtraBlockAsset[] ExtraBlocks { get; private set; }
	public static PlayerAbility[] PlayerAbilities { get; private set; }
	public static TankPartAsset[] TankParts { get; private set; }
	public static Dictionary<WorldTheme, List<BlockAsset>> BlockAssets { get; private set; }
	public static async UniTask LoadThemeAssets() {
		var waiter = await Addressables.LoadAssetsAsync<ThemeAsset>("Themes", null);
		ThemeAssets = new ThemeAsset[waiter.Count];

		for (int i = 0; i < ThemeAssets.Length; i++) {
			ThemeAssets[i] = waiter[i];
		}
		ThemeAssets = ThemeAssets.OrderBy(t => (int)t.theme).ToArray();
	}
	public static async UniTask LoadTankAssets() {
		var waiter = await Addressables.LoadAssetsAsync<TankAsset>("Tanks", null);
		TankAssets = new TankAsset[waiter.Count];

		for(int i = 0; i< TankAssets.Length; i++) {
			TankAssets[i] = waiter[i];
		}
		TankAssets = TankAssets.OrderBy(t => (int)t.tankType).ToArray();
	}
	public static async UniTask LoadLevelAssets() {
		var waiter = await Addressables.LoadAssetsAsync<TextAsset>("Levels", null);
		LevelAssets = new LevelData[waiter.Count];

		for (int i = 0; i < LevelAssets.Length; i++) {
			try {
				LevelAssets[i] = JsonConvert.DeserializeObject<LevelData>(waiter[i].text, new JsonSerializerSettings() {
					Formatting = Formatting.None,
					TypeNameHandling = TypeNameHandling.Auto,
				});
			} catch {
				LevelAssets[i] = JsonConvert.DeserializeObject<LevelData>(Game.DecompressString(waiter[i].text), new JsonSerializerSettings() {
					Formatting = Formatting.None,
					TypeNameHandling = TypeNameHandling.Auto,
				});
			}
		}
		LevelAssets = LevelAssets.OrderBy(t => (int)t.levelId).ToArray();
	}
	public static async UniTask LoadGroundTileAssets() {
		var waiter =await Addressables.LoadAssetsAsync<GroundTileData>("GroundTiles", null);
		GroundTiles = new GroundTileData[waiter.Count];

		for (int i = 0; i < GroundTiles.Length; i++) {
			GroundTiles[i] = waiter[i];
		}
		GroundTiles = GroundTiles.OrderBy(t => (int)t.type).ToArray();
	}
	public static async UniTask LoadBlockAssets() {
		BlockAssets = new Dictionary<WorldTheme, List<BlockAsset>>();

		foreach (var thema in Enum.GetValues(typeof(WorldTheme))) {
			try {
				var thWaiter = await Addressables.LoadAssetsAsync<BlockAsset>(thema.ToString() + "Assets", null);
				var list = new List<BlockAsset>();

				for (int i = 0; i < thWaiter.Count; i++) {
					list.Add(thWaiter[i]);
				}
				list = list.OrderBy(a => (int)a.block).ToList();
				BlockAssets.Add((WorldTheme)Enum.Parse(typeof(WorldTheme), thema.ToString()), list);
			} catch {
				Debug.LogWarning("Could not load block assets from " + thema);
			}
		}
	}
	public static async UniTask LoadLevelPreviews() {
		try {
			var waiter = await Addressables.LoadAssetsAsync<Sprite>("LevelPreviews", null);
			LevelPreviews = new Sprite[waiter.Count];

			for (int i = 0; i < LevelPreviews.Length; i++) {
				LevelPreviews[i] = waiter[i];
			}
		} catch {
			Debug.LogError("An error occurred to load the Level-Preview Images.");
		}
	}
	public static async UniTask LoadExtraBlocks() {
		var waiter = await Addressables.LoadAssetsAsync<ExtraBlockAsset>("ExtraBlocks", null);
		ExtraBlocks = new ExtraBlockAsset[waiter.Count];

		for (int i = 0; i < ExtraBlocks.Length; i++) {
			ExtraBlocks[i] = waiter[i];
		}
		ExtraBlocks = ExtraBlocks.OrderBy(t => (int)t.block).ToArray();
	}
	public static async UniTask LoadFloraBlocks() {
		var waiter = await Addressables.LoadAssetsAsync<FloraAsset>("FloraBlocks", null);
		FloraBlocks = new FloraAsset[waiter.Count];

		for (int i = 0; i < FloraBlocks.Length; i++) {
			FloraBlocks[i] = waiter[i];
		}
		FloraBlocks = FloraBlocks.OrderBy(t => (int)t.block).ToArray();
	}
	public static async UniTask LoadCombatAbilities() {
		var waiter = await Addressables.LoadAssetsAsync<PlayerAbility>("CombatAbilities", null);
		PlayerAbilities = new PlayerAbility[waiter.Count];

		for (int i = 0; i < PlayerAbilities.Length; i++) {
			PlayerAbilities[i] = waiter[i];
		}
		PlayerAbilities = PlayerAbilities.OrderBy(t => (int)t.ability).ToArray();
	}
	public static async UniTask LoadTankParts() {
		var waiter = await Addressables.LoadAssetsAsync<TankPartAsset>("TankParts", null);
		TankParts = new TankPartAsset[waiter.Count];

		for (int i = 0; i < TankParts.Length; i++) {
			TankParts[i] = waiter[i];
		}
		TankParts = TankParts.OrderBy(t => (int)t.type).ToArray();
	}
	public static async UniTask PreloadAssetsStartup() {
		await GameStartup.SetLoadingText("Loading Tanks");
		await LoadTankAssets();
		Logger.Log(Channel.System, "Loaded Tanks: " + TankAssets.Length);
		
		await GameStartup.SetLoadingText("Loading Levels");
		await LoadLevelAssets();
		Logger.Log(Channel.System, "Loaded Levels: " + LevelAssets.Length);

		await GameStartup.SetLoadingText("Loading Abilities");
		await LoadCombatAbilities();
		Logger.Log(Channel.System, "Loaded Abilites: " + PlayerAbilities.Length);

		await GameStartup.SetLoadingText("Loading Ground-Tiles");
		await LoadGroundTileAssets();
		Logger.Log(Channel.System, "Loaded GroundTiles: " + GroundTiles.Length);

		await GameStartup.SetLoadingText("Loading Blocks");
		await LoadBlockAssets();
		Logger.Log(Channel.System, "Loaded Blocks: " + BlockAssets.Count);

		await GameStartup.SetLoadingText("Loading more Blocks");
		await LoadExtraBlocks();
		Logger.Log(Channel.System, "Loaded ExtraBlocks: " + ExtraBlocks.Length);

		await GameStartup.SetLoadingText("Loading Flora");
		await LoadFloraBlocks();
		Logger.Log(Channel.System, "Loaded FloraBlocks: " + FloraBlocks.Length);

		await GameStartup.SetLoadingText($"Loading Level-Screenshots");
		await LoadLevelPreviews();
		Logger.Log(Channel.System, "Loaded Level Previews: " + LevelPreviews.Length);

		await GameStartup.SetLoadingText("Loading Tank Assemblies");
		await LoadTankParts();
		Logger.Log(Channel.System, "Loaded Tank Assemblies: " + TankParts.Length);

		await GameStartup.SetLoadingText("Loading Worlds");
		await LoadThemeAssets();
		Logger.Log(Channel.System, "Loaded Worlds: " + ThemeAssets.Length);

		await UniTask.Delay(50);
		Logger.Log(Channel.System, "Loading Physic Materials");
		BounceFriction = await Addressables.LoadAssetAsync<PhysicMaterial>("BounceFriction");
		NoFriction = await Addressables.LoadAssetAsync<PhysicMaterial>("NoFriction");
		DefaultFriction = await Addressables.LoadAssetAsync<PhysicMaterial>("DefaultFriction");
	}

	#region Getters
	public static ThemeAsset GetTheme(WorldTheme theme) => ThemeAssets.ToList().Find(t => t.theme == theme);
	public static BlockAsset GetBlockAsset(WorldTheme theme, BlockType type) => BlockAssets[theme].Find(a => a.block == type);
	public static List<BlockAsset> GetBlockAssets(WorldTheme theme) => BlockAssets[theme];
	public static TankAsset GetTank(TankTypes type) => TankAssets.ToList().Find(t => t.tankType == type);
	public static LevelData GetOfficialLevel(ulong level) => LevelAssets.ToList().Find(l => l.levelId == level);
	public static GroundTileData GetGroundTile(GroundTileType type) => GroundTiles.ToList().Find(t => t.type == type);
	public static Sprite GetOfficialLevelPreview(ulong level) => LevelPreviews[(int)level];
	public static ExtraBlockAsset[] GetExtraBlockAssets() => ExtraBlocks;
	public static FloraAsset[] GetFloraAssets() => FloraBlocks;
	public static PlayerAbility GetCombatAbility(CombatAbility ability) => PlayerAbilities.ToList().Find(ab => ab.ability == ability);
	public static PlayerAbility[] GetCombatAbilities() => PlayerAbilities;
	public static FloraAsset GetFloraAsset(FloraBlocks type) => GetFloraAssets().ToList().Find(t => t.block == type);
	public static ExtraBlockAsset GetExtraBlockAsset(ExtraBlocks block) => GetExtraBlockAssets().ToList().Find(t => t.block == block);
	public static TankPartAsset[] GetParts(TankPartAsset.TankPartType type) => TankParts.Where(p => p.type == type).OrderBy(p => p.id).ToArray();
	public static TankPartAsset GetPart(TankPartAsset.TankPartType type, int index) => GetParts(type)[index];
	#endregion
}
