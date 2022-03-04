using UnityEngine;
using System.IO;
using IniParser;
using IniParser.Model;
using Sperlich.Types;
using System;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
// HDRP Related: using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Events;
using ToyTanks.UI;
using SimpleMan.Extensions;
using UnityEngine.Rendering.Universal;
using Sperlich.PrefabManager;
using DG.Tweening;
using LeTai.Asset.TranslucentImage;

public class GraphicSettings : MonoBehaviour {

    public enum ShadowQualities { High, Medium, Low, Off }
    public enum TextureQualities { High, Medium, Low, Minimal }
    public enum WindowMode { Fullscreen, Windowed }
    public enum AOQualities { High, Low, Off }
    private static GraphicSettings _instance;
    public static GraphicSettings Instance {
        get {
            if (_instance == null) {
                if(FindObjectOfType<GraphicSettings>()) {
                    _instance = FindObjectOfType<GraphicSettings>();
                    return _instance;
                }
                _instance = PrefabManager.Instantiate<GraphicSettings>(PrefabTypes.GraphicSettings);
                _instance.name = "GraphicSettings";
            }
            return _instance;
        }
    }
    public static Int2 nativeScreenResolution;
    // User Interface
    public GameObject desktopUI;
    public GameObject mobileUI;
    public CanvasGroup graphicsMenu;
    public GameObject fpsCounter;
    public UniversalRendererData rendererData;
    public UniversalRenderPipelineAsset pipeline;
    public Volume performanceOverride;
    // Audio Preview
    public AudioClip mainVolumePreview;
    public AudioClip soundEffectsVolumePreview;
    bool lockAudioPreview;

    // UI Interface
    public static GraphicItem controlScheme => GetOption("ControlSchemeUI");
    public static GraphicItem textureQualityChoose => GetOption("TextureQualityUI");
    public static GraphicItem shadowQualityChoose => GetOption("ShadowQualityUI");
    public static GraphicItem windowModeChoose => GetOption("WindowModeUI");
    public static GraphicItem windowResolutionChoose => GetOption("WindowResolutionUI");
    public static GraphicItem mainVolumeSlider => GetOption("MainVolumeUI");
    public static GraphicItem soundEffectsVolumeSlider => GetOption("SoundEffectsVolumeUI");
    public static GraphicItem antialiasingToggle => GetOption("AntialisingQualityUI");
    public static GraphicItem ambientOcclusionToggle => GetOption("AmbientOcclusionQualityUI");
    public static GraphicItem performanceModeToggle => GetOption("PerformanceModeUI");
    public static GraphicItem vsyncToggle => GetOption("VsyncUI");

    // Logics
    static IniData LoadedSettings;
    static string AudioSettings => "Audio";
    static string WindowSettings => "WindowSettings";
    static string Graphics => "Graphics";
    static string Controls => "Controls";
    public static int MainVolume {
        get => ParseInt(GetValue(AudioSettings, nameof(MainVolume)));
        set => SetValue(AudioSettings, nameof(MainVolume), value);
	}
    public static int MusicVolume {
        get => ParseInt(GetValue(AudioSettings, nameof(MusicVolume)));
        set => SetValue(AudioSettings, nameof(MusicVolume), value);
    }
    public static int SoundEffectsVolume {
        get => ParseInt(GetValue(AudioSettings, nameof(SoundEffectsVolume)));
        set => SetValue(AudioSettings, nameof(SoundEffectsVolume), value);
    }
    public static int WindowSizeX {
        get => ParseInt(GetValue(WindowSettings, nameof(WindowSizeX)));
        set => SetValue(WindowSettings, nameof(WindowSizeX), value);
    }
    public static int WindowSizeY {
        get => ParseInt(GetValue(WindowSettings, nameof(WindowSizeY)));
        set => SetValue(WindowSettings, nameof(WindowSizeY), value);
    }
    public static bool Antialiasing {
        get => ParseBool(GetValue(Graphics, nameof(Antialiasing)));
        set => SetValue(Graphics, nameof(Antialiasing), value);
	}
    public static int AmbientOcclusion {
        get => ParseInt(GetValue(Graphics, nameof(AmbientOcclusion)));
        set => SetValue(Graphics, nameof(AmbientOcclusion), value);
    }
    public static bool PerformanceMode {
        get => ParseBool(GetValue(Graphics, nameof(PerformanceMode)));
        set => SetValue(Graphics, nameof(PerformanceMode), value);
    }
    public static bool VSYNC {
        get => ParseBool(GetValue(Graphics, nameof(VSYNC)));
        set => SetValue(Graphics, nameof(VSYNC), value);
	}
    public static int TextureQuality {
        get => ParseInt(GetValue(Graphics, nameof(TextureQuality)));
        set => SetValue(Graphics, nameof(TextureQuality), value);
	}
    public static float UIScale {
        get => ParseFloat(GetValue(Graphics, nameof(UIScale)));
        set => SetValue(Graphics, nameof(UIScale), value);
    }
    public static int ShadowQuality {
        get => ParseInt(GetValue(Graphics, nameof(ShadowQuality)));
        set => SetValue(Graphics, nameof(ShadowQuality), value);
	}
    static int FullscreenMode {
        get => ParseInt(GetValue(WindowSettings, nameof(FullscreenMode)));
        set {
            SetValue(WindowSettings, nameof(FullscreenMode), (int)value);
        }
	}
    static int ControlScheme {
        get => ParseInt(GetValue(Controls, nameof(ControlScheme)));
        set {
            SetValue(Controls, nameof(ControlScheme), (int)value);
        }
    }

    private void Awake() {
        _instance = this;
        transform.SetParent(null);
        nativeScreenResolution = new Int2(Screen.currentResolution.width, Screen.currentResolution.height);
        graphicsMenu.alpha = 0;
        graphicsMenu.gameObject.SetActive(false);
    }
	private void Update() {
        if(Instance != null && Instance.graphicsMenu.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
            CloseOptionsMenu();
		}
    }
	/// <summary>
	/// Initialized the Graphic Settings
	/// </summary>
	public static void Initialize() {
        _instance = PrefabManager.Instantiate<GraphicSettings>(PrefabTypes.GraphicSettings);
        DontDestroyOnLoad(Instance.gameObject);
        Instance.AdjustToPlatform();
        if(File.Exists(GamePaths.UserGraphicSettings) == false) {
            // Create new User-Settings
            LoadedSettings = new IniData();
            VerifyGraphicSettings();
            Logger.Log(Channel.Graphics, "No graphics.ini file found. Creating new one.");
        } else {
            LoadSettings();
		}
        ApplySettings();

        Logger.Log(Channel.System, "GraphicsManager has been initialized.");
    }
    private void AdjustToPlatform() {
        if(Game.Platform == GamePlatform.Desktop) {
            desktopUI.SetActive(true);
            mobileUI.SetActive(false);
        } else if(Game.Platform == GamePlatform.Mobile) {
            desktopUI.SetActive(false);
            mobileUI.SetActive(true);
        }
	}

	#region UI-Methods
	public static void OpenOptionsMenu() {
        LoadSettings();
        Instance.graphicsMenu.gameObject.SetActive(true);
        Instance.LockAudioPreview(1f);

        // Initialize GraphicItems
        textureQualityChoose.Init<TextureQualities>(TextureQuality);
        shadowQualityChoose.Init<ShadowQualities>(ShadowQuality);
        windowModeChoose.Init<WindowMode>(FullscreenMode);
        mainVolumeSlider.Init(MainVolume);
        soundEffectsVolumeSlider.Init(SoundEffectsVolume);
        antialiasingToggle.Init(Antialiasing);
        ambientOcclusionToggle.Init<AOQualities>(AmbientOcclusion);
        performanceModeToggle.Init(PerformanceMode);
        vsyncToggle.Init(VSYNC);
        windowResolutionChoose.Init(new Int2(WindowSizeX, WindowSizeY), Screen.resolutions);

        DOTween.To(() => Instance.graphicsMenu.alpha, x => Instance.graphicsMenu.alpha = x, 1, 0.2f).OnComplete(() => {
            RefreshUI();
            Logger.Log(Channel.Graphics, "Displaying Graphics menu.");
        }).SetUpdate(true);
    }
    public static void CloseOptionsMenu(float duration = 0f) {
        if (duration == 0) {
            duration = 0.002f;
        }
        DOTween.To(() => Instance.graphicsMenu.alpha, x => Instance.graphicsMenu.alpha = x, 0, duration).OnComplete(() => {
            if (GameObject.Find("MainMenu")) {
                GameObject.Find("MainMenu").GetComponent<MenuItem>().FadeIn();
            }
            Instance.graphicsMenu.gameObject.SetActive(false);
        }).SetUpdate(true);
        MenuManager.Instance?.FadeOutBlur();
        Logger.Log(Channel.Graphics, "Closing Graphics menu.");
    }
    public static void RefreshUI() {
        if (Game.Platform == GamePlatform.Desktop) {
            
        } else if (Game.Platform == GamePlatform.Mobile) {
            
        }
    }
    public static GraphicItem GetOption(string tagName) {
        var g = GameObject.FindGameObjectWithTag(tagName);
        if(g == null) {
            throw new NullReferenceException("Option with the tag " + tagName + " could not be found.");
        }
        if(g.transform.TrySearchComponent(out GraphicItem item) && item != null) {
            return item;
		}
        throw new NullReferenceException("The component <GraphicItem> could not be found on the GameObject.");
    }
	#endregion

	#region Apply Settings
	public static void ApplySettings() {
        if(Game.Platform == GamePlatform.Desktop) {
            SetWindowResolution(new Int2(WindowSizeX, WindowSizeY));
            SetGameWindowMode(FullscreenMode);
            SetTextureResolution(TextureQuality);
            SetShadowResolution(ShadowQuality);
            SetAntialiasing(Antialiasing);
            SetMainVolume(MainVolume);
            SetSoundEffectsVolume(SoundEffectsVolume);
            SetAmbientOcclusion(AmbientOcclusion);
            SetVsync(VSYNC);
            SetPerformanceMode(PerformanceMode);
            //SetControlScheme(PlayerControlSchemes.Desktop);
        } else if(Game.Platform == GamePlatform.Mobile) {
            SetTextureResolution(TextureQuality);
            SetShadowResolution(ShadowQuality);
            SetAntialiasing(Antialiasing);
            SetMainVolume(MainVolume);
            SetSoundEffectsVolume(SoundEffectsVolume);
            SetAmbientOcclusion(AmbientOcclusion);
            SetPerformanceMode(PerformanceMode);
            //SetControlScheme(ControlScheme);
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
        }
    }
    public static void SetMainVolume(int volume) {
        MainVolume = volume;
		AudioListener.volume = volume / 100f;
	}
    public static void SetMusicVolume(int volume) {
        MusicVolume = volume;
	}
    public static void SetSoundEffectsVolume(int volume) {
		SoundEffectsVolume = volume;
	}
    public static void SetWindowResolution(Resolution res) => SetWindowResolution(new Int2(res.width, res.height));
    public static void SetWindowResolution(Int2 resolution) {
        WindowSizeX = resolution.x;
        WindowSizeY = resolution.y;
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode);
    }
    public static void SetGameWindowMode(int mode) {
        FullscreenMode = mode;
		switch((WindowMode)mode) {
			case WindowMode.Fullscreen:
                Screen.fullScreen = true;
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
			case WindowMode.Windowed:
                Screen.fullScreen = false;
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
		}
	}
    public static void SetControlScheme(int scheme) {
        ControlScheme = scheme;
        Game.PlayerControlScheme = (PlayerControlSchemes)scheme;
    }
    public static void SetAntialiasing(bool state) {
        foreach(var caminfo in FindObjectsOfType<UniversalAdditionalCameraData>()) {
            if(state) {
                caminfo.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            } else {
                caminfo.antialiasing = AntialiasingMode.None;
            }
        }
        Antialiasing = state;
    }
    public static void SetAmbientOcclusion(int state) {
		switch(state) {
            case 0:
                ExtraGraphics.AO_SampleRate = 6;
                ExtraGraphics.AO_Quality = 2;
                ExtraGraphics.AO_Downsample = false;
                GetAORendererFeature().SetActive(true);
                break;
            case 1:
                ExtraGraphics.AO_Quality = 0;
                ExtraGraphics.AO_SampleRate = 4;
                ExtraGraphics.AO_Downsample = true;
                GetAORendererFeature().SetActive(true);
                break;
            case 2:
                GetAORendererFeature().SetActive(false);
                break;
		}
		AmbientOcclusion = state;
	}
    public static void SetPerformanceMode(bool state) {
        if(state) {
            Instance.pipeline.renderScale = 0.75f;
            Instance.performanceOverride.gameObject.SetActive(true);
            foreach(var d in FindObjectsOfType<DecalProjector>()) {
                if(d.enabled) {
                    d.enabled = false;
				}
            }
            foreach(var t in FindObjectsOfType<TranslucentImageSource>()) {
                t.Downsample = 2;
                t.maxUpdateRate = 24;
            }
        } else {
            Instance.pipeline.renderScale = 1f;
            Instance.performanceOverride.gameObject.SetActive(false);
            foreach(var t in FindObjectsOfType<TranslucentImageSource>()) {
                t.Downsample = 1;
                t.maxUpdateRate = 60;
            }
        }
        PerformanceMode = state;
	}
    public static void SetVsync(bool state) {
        VSYNC = state;
        if(state) {
            QualitySettings.vSyncCount = 1;
		} else {
            QualitySettings.vSyncCount = 0;
        }
	}
    public static void SetTextureResolution(int resolution) {
        TextureQuality = resolution;
        QualitySettings.masterTextureLimit = resolution;
    }
    public static void SetShadowResolution(int level) {
        // Apply Shadow Resolution to all light sources
        switch(level) {
            case 0:
                ExtraGraphics.MainLightCastShadows = true;
                ExtraGraphics.AdditionalLightCastShadows = true;
                ExtraGraphics.SoftShadowsEnabled = true;
                ExtraGraphics.MainLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._4096;
                ExtraGraphics.AdditionalLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._2048;
                break;
            case 1:
                ExtraGraphics.MainLightCastShadows = true;
                ExtraGraphics.AdditionalLightCastShadows = true;
                ExtraGraphics.SoftShadowsEnabled = true;
                ExtraGraphics.MainLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._2048;
                ExtraGraphics.AdditionalLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._2048;
                break;
            case 2:
                ExtraGraphics.MainLightCastShadows = true;
                ExtraGraphics.AdditionalLightCastShadows = false;
                ExtraGraphics.SoftShadowsEnabled = true;
                ExtraGraphics.MainLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._1024;
                ExtraGraphics.AdditionalLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._1024;
                break;
            case 3:
                ExtraGraphics.MainLightCastShadows = false;
                ExtraGraphics.AdditionalLightCastShadows = false;
                ExtraGraphics.SoftShadowsEnabled = false;
                ExtraGraphics.MainLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._256;
                ExtraGraphics.AdditionalLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._256;
                break;
        }
        ShadowQuality = level;
    }
	#endregion

	#region Functional
	public void PlayMainVolume() {
		if(lockAudioPreview == false) {
			AudioPlayer.Play(mainVolumePreview.name, AudioType.Default);
			LockAudioPreview(mainVolumePreview.length);
		}
	}
	public void PlaySoundEffectsVolume() {
		if(lockAudioPreview == false) {
			AudioPlayer.Play(soundEffectsVolumePreview.name, AudioType.SoundEffect);
			LockAudioPreview(soundEffectsVolumePreview.length);
		}
	}
	public void LockAudioPreview(float duration) {
		lockAudioPreview = true;
		this.Delay(duration, () => lockAudioPreview = false);
	}

    public static void SaveSettings() {
        var parser = new FileIniDataParser();
        parser.WriteFile(GamePaths.UserGraphicSettings, LoadedSettings);
    }
    public static void LoadSettings() {
        var parser = new FileIniDataParser();
        LoadedSettings = parser.ReadFile(GamePaths.UserGraphicSettings);

        // Check if variables are set
        VerifyGraphicSettings();
        ApplySettings();
    }
    public static void ResetSettings() {
        File.Delete(GamePaths.UserGraphicSettings);
        Initialize();
	}
    public static void VerifyGraphicSettings() {
        if(GetValue(AudioSettings, nameof(MainVolume)) is null) {
            MainVolume = 60;
            Logger.Log(Channel.Graphics, "No MainVolume setting found. Restoring to default.");
        }
        if(GetValue(AudioSettings, nameof(SoundEffectsVolume)) is null) {
            SoundEffectsVolume = 40;
            Logger.Log(Channel.Graphics, "No SoundEffectsVolume setting found. Restoring to default.");
        }
        if(GetValue(WindowSettings, nameof(FullscreenMode)) is null) {
            FullscreenMode = (int)WindowMode.Fullscreen;
            Logger.Log(Channel.Graphics, "No WindowSettings setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(Antialiasing)) is null) {
            Antialiasing = true;
            Logger.Log(Channel.Graphics, "No Antialising setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(VSYNC)) is null) {
            VSYNC = true;
            Logger.Log(Channel.Graphics, "No VSYNC setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(ShadowQuality)) is null) {
            ShadowQuality = (int)ShadowQualities.High;
            Logger.Log(Channel.Graphics, "No ShadowQuality setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(TextureQuality)) is null) {
            TextureQuality = (int)TextureQualities.High;
            Logger.Log(Channel.Graphics, "No TextureQuality setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(UIScale)) is null) {
            UIScale = 1;
            Logger.Log(Channel.Graphics, "No UIScale setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(AmbientOcclusion)) is null) {
            AmbientOcclusion = 1;
            Logger.Log(Channel.Graphics, "No AmbientOcclusion setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(PerformanceMode)) is null) {
            PerformanceMode = false;
            Logger.Log(Channel.Graphics, "No PerformanceMode setting found. Restoring to default.");
        }
        if(GetValue(WindowSettings, nameof(WindowSizeX)) is null) {
            WindowSizeX = Screen.currentResolution.width;
            Logger.Log(Channel.Graphics, "No WindowSizeX setting found. Restoring to default.");
        }
        if(GetValue(WindowSettings, nameof(WindowSizeY)) is null) {
            WindowSizeY = Screen.currentResolution.height;
            Logger.Log(Channel.Graphics, "No WindowSizeY setting found. Restoring to default.");
        }
    }
	#endregion

	#region Conversion/Helpers
	static int ParseInt(string value) {
        try {
            if(value is null) {
                return 0;
            } else {
                return int.Parse(value);
            }
		} catch {
            Logger.Log(Channel.Graphics, "Failed to read INT value " + value);
            throw new GraphicSettingsNotFound("Failed to read INT value: " + value);
		}
	}
    static float ParseFloat(string value) {
        try {
            if(value is null) {
                return 1;
            } else {
                return float.Parse(value);
            }
        } catch {
            Logger.Log(Channel.Graphics, "Failed to read FLOAT value " + value);
            throw new GraphicSettingsNotFound("Failed to read FLOAT value: " + value);
        }
    }
    static bool ParseBool(string value) {
        try {
            if(value is null) {
                return false;
            } else {
                return bool.Parse(value);
            }
        } catch {
            Logger.Log(Channel.Graphics, "Failed to read BOOLEAN value " + value);
            throw new GraphicSettingsNotFound("Failed to read BOOLEAN value: " + value);
        }
    }
    static void SetValue(string section, string parameter, object value) {
        try {
            LoadedSettings[section][parameter] = value.ToString();
            SaveSettings();
		} catch {
            Logger.Log(Channel.Graphics, "Failed to set parameter " + parameter + " value.");
            throw new GraphicSettingsNotFound("Failed Setting parameter: " + parameter + " to " + value.ToString());
		}
	}
    static string GetValue(string section, string parameter) {
        return LoadedSettings[section][parameter];
	}
    static Int2 GetScreenResolution(SupportedScreenSizes size) {
        string s = size.ToString().Replace("Size_", "");
        int width = ParseInt(s.Split('x')[0]);
        int height = ParseInt(s.Split('x')[1]);
        return new Int2(width, height);
    }
    static SupportedScreenSizes GetSupportedScreenSize(Int2 size) {
        int count = 0;
        foreach(var ssize in Enum.GetValues(typeof(SupportedScreenSizes))) {
            string s = ssize.ToString().Replace("Size_", "");
            int width = ParseInt(s.Split('x')[0]);
            int height = ParseInt(s.Split('x')[1]);
            if(width == size.x && height == size.y) {
                return (SupportedScreenSizes)count;
			}
            count++;
		}
        Logger.Log(Channel.Graphics, "No supported resolution has been found. Returning default resolution FullHD");
        return SupportedScreenSizes.Size_1920x1080;
	}
    static int GetShadowResolution(int level) {
		switch(level) {
            case 0:
                return 4096;
            case 1:
                return 2048;
            case 2:
                return 1024;
            case 3:
                return 512;
			case 4:
				return 0; 
        }
        return 2048;
	}
    public static int GetShadowResolution() => GetShadowResolution(ShadowQuality);
    public static ScriptableRendererFeature GetAORendererFeature() {
        return Instance.rendererData.rendererFeatures.Find(r => r.name == "Ambient Occlusion");
    }
    public static ScriptableRendererFeature GetDecalRendererFeature() {
        return Instance.rendererData.rendererFeatures.Find(r => r.name == "DecalRendererFeature");
    }
    #endregion
}