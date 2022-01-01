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

public class GraphicSettings : MonoBehaviour {

    private static GraphicSettings _instance;
    public static GraphicSettings Instance {
        get {
            if (_instance == null) {
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
    // Audio Preview
    public AudioClip mainVolumePreview;
    public AudioClip soundEffectsVolumePreview;
    bool lockAudioPreview;

    // UI Interface
    public HorizontalChoose controlScheme => GameObject.FindGameObjectWithTag("ControlSchemeUI").transform.SearchComponent<HorizontalChoose>();
    public HorizontalChoose textureQualityChoose => GameObject.FindGameObjectWithTag("TextureQualityUI").transform.SearchComponent<HorizontalChoose>();
    public HorizontalChoose shadowQualityChoose => GameObject.FindGameObjectWithTag("ShadowQualityUI").transform.SearchComponent<HorizontalChoose>();
    public HorizontalChoose windowModeDropdown => GameObject.FindGameObjectWithTag("WindowModeUI").transform.SearchComponent<HorizontalChoose>();
    public TMP_Dropdown windowResolutionDropdown => GameObject.FindGameObjectWithTag("WindowResolutionUI").transform.SearchComponent<TMP_Dropdown>();
    public SliderWithText mainVolumeSlider => GameObject.FindGameObjectWithTag("MainVolumeUI").transform.SearchComponent<SliderWithText>();
    public SliderWithText soundEffectsVolumeSlider => GameObject.FindGameObjectWithTag("SoundEffectsVolumeUI").transform.SearchComponent<SliderWithText>();
    public Toggle antialiasingToggle => GameObject.FindGameObjectWithTag("AntialisingQualityUI").transform.SearchComponent<Toggle>();
    public Toggle ambientOcclusionToggle => GameObject.FindGameObjectWithTag("AmbientOcclusionQualityUI").transform.SearchComponent<Toggle>();
    public Toggle performanceModeToggle => GameObject.FindGameObjectWithTag("PerformanceModeUI").transform.SearchComponent<Toggle>();
    public Toggle vsyncToggle => GameObject.FindGameObjectWithTag("VsyncUI").transform.SearchComponent<Toggle>();

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
    static bool Antialiasing {
        get => ParseBool(GetValue(Graphics, nameof(Antialiasing)));
        set => SetValue(Graphics, nameof(Antialiasing), value);
	}
    static bool AmbientOcclusion {
        get => ParseBool(GetValue(Graphics, nameof(AmbientOcclusion)));
        set => SetValue(Graphics, nameof(AmbientOcclusion), value);
    }
    static bool PerformanceMode {
        get => ParseBool(GetValue(Graphics, nameof(PerformanceMode)));
        set => SetValue(Graphics, nameof(PerformanceMode), value);
    }
    static bool SSR {
        get => ParseBool(GetValue(Graphics, nameof(SSR)));
        set => SetValue(Graphics, nameof(SSR), value);
	}
    static bool VSYNC {
        get => ParseBool(GetValue(Graphics, nameof(VSYNC)));
        set => SetValue(Graphics, nameof(VSYNC), value);
	}
    static int TextureQuality {
        get => ParseInt(GetValue(Graphics, nameof(TextureQuality)));
        set => SetValue(Graphics, nameof(TextureQuality), value);
	}
    static float UIScale {
        get => ParseFloat(GetValue(Graphics, nameof(UIScale)));
        set => SetValue(Graphics, nameof(UIScale), value);
    }
    public static int ShadowQuality {
        get => ParseInt(GetValue(Graphics, nameof(ShadowQuality)));
        set => SetValue(Graphics, nameof(ShadowQuality), value);
	}
    static bool SoftShadows {
        get => ParseBool(GetValue(Graphics, nameof(SoftShadows)));
        set => SetValue(Graphics, nameof(SoftShadows), value);
	}
    static SupportedScreenSizes WindowGameSize {
        get {
            Int2 screenSize = new Int2(ParseInt(GetValue(WindowSettings, nameof(WindowGameSize) + "X")), ParseInt(GetValue(WindowSettings, nameof(WindowGameSize) + "Y")));
            Debug.Log(screenSize);
            return GetSupportedScreenSize(screenSize);
		}
        set {
            Int2 resolution = GetScreenResolution(value);
            SetValue(WindowSettings, nameof(WindowGameSize) + "X", resolution.x);
            SetValue(WindowSettings, nameof(WindowGameSize) + "Y", resolution.y);
        }
	}
    static FullScreenMode FullscreenMode {
        get => (FullScreenMode)ParseInt(GetValue(WindowSettings, nameof(FullscreenMode)));
        set {
            SetValue(WindowSettings, nameof(FullscreenMode), (int)value);
        }
	}
    static PlayerControlSchemes ControlScheme {
        get => (PlayerControlSchemes)ParseInt(GetValue(Controls, nameof(ControlScheme)));
        set {
            SetValue(Controls, nameof(ControlScheme), (int)value);
        }
    }

    protected void Awake() {
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

    // UI-Interface Methods
    public static void RenderResolutionDropdown() {
        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var size in Enum.GetValues(typeof(SupportedScreenSizes))) {
            string s = size.ToString().Replace("Size_", "");
            int width = ParseInt(s.Split('x')[0]);
            int height = ParseInt(s.Split('x')[1]);

            if (width > nativeScreenResolution.x || height > nativeScreenResolution.y) {
                continue;
            }
            var item = new TMP_Dropdown.OptionData() {
                text = size.ToString().Replace("Size_", ""),
            };
            options.Add(item);
        }
        options.Reverse();
        Instance.windowResolutionDropdown.options = options;
        Instance.windowResolutionDropdown.RefreshShownValue();
    }
    public static void RenderWindowModeDropdown() {
        Instance.windowModeDropdown.objs = new List<string>() { "Fullscreen", "Borderless", "Window" };
        Instance.windowModeDropdown.index = TextureQuality;
    }
    public static void RenderTextureResolutionsDropdown() {
        Instance.textureQualityChoose.objs = new List<string>() { "High", "Medium", "Low" };
        Instance.textureQualityChoose.index = TextureQuality;
    }
    public static void RenderShadowResolutionDropdown() {
        Instance.shadowQualityChoose.objs = new List<string>() { "High", "Low", "Off" };
        Instance.shadowQualityChoose.index = ShadowQuality;
    }
    public static void RenderControlSchemes() {
        Instance.controlScheme.objs = new List<string>() { "Double DPad", "DPad and Tap" };
        Instance.controlScheme.index = (int)ControlScheme;
    }
    public static void OpenOptionsMenu(float duration = 0.1f) {
        if(Instance == null) {
            _instance = FindObjectOfType<GraphicSettings>();
        }
        Instance.graphicsMenu.gameObject.SetActive(true);
        Instance.LockAudioPreview(1f);
        LoadSettings();
        DOTween.To(() => Instance.graphicsMenu.alpha, x => Instance.graphicsMenu.alpha = x, 1, duration).OnComplete(() => {
            RefreshUI();
            Logger.Log(Channel.Graphics, "Displaying Graphics menu.");
        });
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
        });
        Logger.Log(Channel.Graphics, "Closing Graphics menu.");
    }
    public static void RefreshUI() {
        if (Game.Platform == GamePlatform.Desktop) {
            Instance.antialiasingToggle.isOn = Antialiasing;
            Instance.shadowQualityChoose.index = ShadowQuality;
            Instance.textureQualityChoose.index = TextureQuality;
            Instance.mainVolumeSlider.SetValue(MainVolume);
            Instance.soundEffectsVolumeSlider.SetValue(SoundEffectsVolume);

            RenderResolutionDropdown();
            RenderWindowModeDropdown();
            RenderTextureResolutionsDropdown();
            RenderShadowResolutionDropdown();
        } else if (Game.Platform == GamePlatform.Mobile) {
            Instance.antialiasingToggle.isOn = Antialiasing;
            Instance.shadowQualityChoose.index = ShadowQuality;
            Instance.textureQualityChoose.index = TextureQuality;
            Instance.mainVolumeSlider.SetValue(MainVolume);
            Instance.soundEffectsVolumeSlider.SetValue(SoundEffectsVolume);

            RenderTextureResolutionsDropdown();
            RenderShadowResolutionDropdown();
            RenderControlSchemes();
        }
    }
    public static void ApplySettings() {
        if (Game.Platform == GamePlatform.Desktop) {
            SetTextureResolution(TextureQuality);
            SetShadowResolution(ShadowQuality);
            SetAntialiasing(Antialiasing);
            SetMainVolume(MainVolume);
            SetSoundEffectsVolume(SoundEffectsVolume);
            SetAmbientOcclusion(AmbientOcclusion);

            SetGameWindowSize(GetScreenResolution(WindowGameSize), FullscreenMode);
            SetVsync(VSYNC);
            SetControlScheme(PlayerControlSchemes.Desktop);
        } else if (Game.Platform == GamePlatform.Mobile) {
            SetTextureResolution(TextureQuality);
            SetShadowResolution(ShadowQuality);
            SetAntialiasing(Antialiasing);
            SetMainVolume(MainVolume);
            SetSoundEffectsVolume(SoundEffectsVolume);
            SetAmbientOcclusion(AmbientOcclusion);
            SetPerformanceMode(PerformanceMode);
            SetControlScheme(ControlScheme);
            Application.targetFrameRate = Screen.currentResolution.refreshRate;
        }
    }

    // Graphic Settings
    static void SetMainVolume(int volume) {
        MainVolume = volume;
		AudioListener.volume = volume / 100f;
	}
    static void SetMusicVolume(int volume) {
        MusicVolume = volume;
	}
	static void SetSoundEffectsVolume(int volume) {
		SoundEffectsVolume = volume;
	}
    static void SetGameWindowSize(SupportedScreenSizes size, FullScreenMode mode) => SetGameWindowSize(GetScreenResolution(size), mode);
    static void SetGameWindowSize(Int2 resolution, FullScreenMode mode) {
        WindowGameSize = GetSupportedScreenSize(resolution);
        Screen.SetResolution(resolution.x, resolution.y, mode);
    }
    static void SetGameWindowMode(FullScreenMode mode) {
        FullscreenMode = mode;
        Screen.fullScreenMode = mode;
		if(mode == FullScreenMode.FullScreenWindow || mode == FullScreenMode.ExclusiveFullScreen) {
			Screen.fullScreen = true;
		} else {
			Screen.fullScreen = false;
		}
	}
    static void SetControlScheme(PlayerControlSchemes scheme) {
        ControlScheme = scheme;
        Game.PlayerControlScheme = scheme;
    }
    static void SetAntialiasing(bool state) {
        foreach(var caminfo in FindObjectsOfType<UniversalAdditionalCameraData>()) {
            if(state) {
                caminfo.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            } else {
                caminfo.antialiasing = AntialiasingMode.None;
            }
        }
        Antialiasing = state;
    }
    static void SetAmbientOcclusion(bool state) {
        for(int x = 0; x < Instance.rendererData.rendererFeatures.Count; x++) {
            if(Instance.rendererData.rendererFeatures[x].name == "Ambient Occlusion") {
                Instance.rendererData.rendererFeatures[x].SetActive(state);
            }
        }
        AmbientOcclusion = state;
	}
    static void SetPerformanceMode(bool state) {
        if(state) {
            Instance.pipeline.renderScale = 0.5f;
		} else {
            Instance.pipeline.renderScale = 1f;
        }
        PerformanceMode = state;
	}
    static void SetVsync(bool state) {
        VSYNC = state;
        if(state) {
            QualitySettings.vSyncCount = 1;
		} else {
            QualitySettings.vSyncCount = 0;
        }
	}
    static void SetTextureResolution(int resolution) {
        TextureQuality = resolution;
        QualitySettings.masterTextureLimit = resolution;
    }
    static void SetShadowResolution(int level) {
        // Apply Shadow Resolution to all light sources
        switch(level) {
            case 0:
                ExtraGraphics.MainLightCastShadows = true;
                ExtraGraphics.AdditionalLightCastShadows = true;
                ExtraGraphics.SoftShadowsEnabled = true;
                ExtraGraphics.MainLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._2048;
                ExtraGraphics.AdditionalLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._1024;
                break;
            case 1:
                ExtraGraphics.MainLightCastShadows = true;
                ExtraGraphics.AdditionalLightCastShadows = false;
                ExtraGraphics.SoftShadowsEnabled = false;
                ExtraGraphics.MainLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._1024;
                ExtraGraphics.AdditionalLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._256;
                break;
            case 2:
                ExtraGraphics.MainLightCastShadows = false;
                ExtraGraphics.AdditionalLightCastShadows = false;
                ExtraGraphics.SoftShadowsEnabled = false;
                ExtraGraphics.MainLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._256;
                ExtraGraphics.AdditionalLightShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution._256;
                break;
        }
        ShadowQuality = level;
    }

    // User interface functions
    public void SetAmbientOcclusionUI() => SetAmbientOcclusion(ambientOcclusionToggle.isOn);
    public void SetAntialiasingUI() => SetAntialiasing(antialiasingToggle.isOn);
    public void SetTextureQualityUI() {
        SetTextureResolution(textureQualityChoose.index);
	}
    public void SetShadowQualityUI() {
        SetShadowResolution(shadowQualityChoose.index);
	}
	public void SetMainVolumeUI() {
		SetMainVolume((int)mainVolumeSlider.Value);
	}
	public void SetSoundEffectsVolumeUI() {
		SetSoundEffectsVolume((int)soundEffectsVolumeSlider.Value);
	}
    public void SetPerformanceModeUI() => SetPerformanceMode(performanceModeToggle.isOn);
    public void SetControlSchemeUI() => SetControlScheme((PlayerControlSchemes)controlScheme.index);
    public void SetWindowModeUI() => SetGameWindowMode((FullScreenMode)windowModeDropdown.index);
    public void SetWindowResolutionUI() => SetGameWindowSize((SupportedScreenSizes)(windowResolutionDropdown.options.Count - 1 - windowResolutionDropdown.value), FullscreenMode);
    public void SetVsync() => SetVsync(vsyncToggle.isOn);

	// Show-Off Test functions
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
        Debug.Log(GetValue(WindowSettings, nameof(WindowGameSize)));
        if(GetValue(WindowSettings, nameof(WindowGameSize)) is null) {
            WindowGameSize = GetSupportedScreenSize(new Int2(Screen.currentResolution.width, Screen.currentResolution.height));
            Logger.Log(Channel.Graphics, "No WindowGameSize setting found. Restoring to default.");
        }
        if(GetValue(WindowSettings, nameof(FullscreenMode)) is null) {
            FullscreenMode = FullScreenMode.FullScreenWindow;
            Logger.Log(Channel.Graphics, "No WindowSettings setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(Antialiasing)) is null) {
            Antialiasing = false;
            Logger.Log(Channel.Graphics, "No Antialising setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(SSR)) is null) {
            SSR = false;
		}
        if(GetValue(Graphics, nameof(VSYNC)) is null) {
            VSYNC = false;
            Logger.Log(Channel.Graphics, "No VSYNC setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(ShadowQuality)) is null) {
            ShadowQuality = 2;
            Logger.Log(Channel.Graphics, "No ShadowQuality setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(UIScale)) is null) {
            UIScale = 1;
            Logger.Log(Channel.Graphics, "No UIScale setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(AmbientOcclusion)) is null) {
            AmbientOcclusion = false;
            Logger.Log(Channel.Graphics, "No AmbientOcclusion setting found. Restoring to default.");
        }
        if(GetValue(Graphics, nameof(PerformanceMode)) is null) {
            PerformanceMode = false;
            Logger.Log(Channel.Graphics, "No PerformanceMode setting found. Restoring to default.");
        }
        if(GetValue(Controls, nameof(ControlScheme)) is null) {
            ControlScheme = PlayerControlSchemes.DpadAndTap;
            Logger.Log(Channel.Graphics, "No ControlScheme setting found. Restoring to default.");
        }
    }

    // Helpers
    static int ParseInt(string value) {
        try {
            if(value is null) {
                return 0;
            } else {
                return int.Parse(value);
            }
		} catch {
            Logger.Log(Channel.Graphics, Priority.Error, "Failed to read INT value " + value);
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
            Logger.Log(Channel.Graphics, Priority.Error, "Failed to read FLOAT value " + value);
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
            Logger.Log(Channel.Graphics, Priority.Error, "Failed to read BOOLEAN value " + value);
            throw new GraphicSettingsNotFound("Failed to read BOOLEAN value: " + value);
        }
    }
    static void SetValue(string section, string parameter, object value) {
        try {
            LoadedSettings[section][parameter] = value.ToString();
            SaveSettings();
		} catch {
            Logger.Log(Channel.Graphics, Priority.Error, "Failed to set parameter " + parameter + " value.");
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
}