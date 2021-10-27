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
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Events;
using ToyTanks.UI;

public class GraphicSettings : MonoBehaviour {

    public static Int2 nativeScreenResolution;
    public static GraphicSettings Instance;
    public static UnityEvent OnGraphicSettingsOpen = new UnityEvent();
    public static UnityEvent OnGraphicSettingsClose = new UnityEvent();
    // User Interface
    public MenuItem menu;
    public Volume overrideVolume;
    public ScreenSpaceReflection volumeSSR;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown windowModeDropdown;
    public TMP_Dropdown textureResolutionDropdown;
    public TMP_Dropdown shadowQualityDropdown;
    public HorizontalChoose textureQualityChoose;
    public HorizontalChoose shadowQualityChoose;
    public SliderWithText uiScaleSlider;
    public Toggle antialiasingToggle;
    public Toggle ssrToggle;
    public Toggle vsyncToggle;
    public Toggle softShadowsToggle;

	void Awake() {
        nativeScreenResolution = new Int2(Screen.currentResolution.width, Screen.currentResolution.height);
        overrideVolume.profile.TryGet(out volumeSSR);
    }

	public static void RenderResolutionDropdown() {
        Instance.resolutionDropdown.ClearOptions();
        Instance.resolutionDropdown.onValueChanged.RemoveAllListeners();
        var options = new List<TMP_Dropdown.OptionData>();

        foreach(var size in Enum.GetValues(typeof(SupportedScreenSizes))) {
            string s = size.ToString().Replace("Size_", "");
            int width = ParseInt(s.Split('x')[0]);
            int height = ParseInt(s.Split('x')[1]);

            if(width > nativeScreenResolution.x || height > nativeScreenResolution.y) {
                continue;
			}
            var item = new TMP_Dropdown.OptionData() {
                text = size.ToString().Replace("Size_", ""),
            };
            options.Add(item);
        }
        options.Reverse();
        Instance.resolutionDropdown.AddOptions(options);
        Instance.resolutionDropdown.value = options.Count - (int)WindowGameSize - 1;
        Instance.resolutionDropdown.RefreshShownValue();
        Instance.resolutionDropdown.onValueChanged.AddListener((int value) => {
            SetGameWindowSize((SupportedScreenSizes)(Instance.resolutionDropdown.options.Count - value - 1), FullscreenMode);
            RefreshUI();
        });
    }
    public static void RenderWindowModeDropdown() {
        Instance.windowModeDropdown.ClearOptions();
        Instance.windowModeDropdown.onValueChanged.RemoveAllListeners();
        var options = new List<TMP_Dropdown.OptionData>();

        foreach(var size in Enum.GetValues(typeof(FullScreenMode))) {
            string text = size.ToString().ToLower();
            switch(text) {
                case "exclusivefullscreen":
                    text = "Fullscreen";
                    break;
                case "fullscreenwindow":
                    continue;
                case "maximizedwindow":
                    text = "Borderless";
                    break;
                case "Windowed":
                    text = "Window";
                    break;
            }
            var item = new TMP_Dropdown.OptionData() {
                text = text,
            };
            options.Add(item);
        }
        Instance.windowModeDropdown.AddOptions(options);
        Instance.windowModeDropdown.value = (int)FullscreenMode;
        Instance.windowModeDropdown.RefreshShownValue();
        Instance.windowModeDropdown.onValueChanged.AddListener((int value) => {
            SetGameWindowMode((FullScreenMode)(value));
            RefreshUI();
        });
    }
    public static void RenderTextureResolutionsDropdown() {
        Instance.textureQualityChoose.objs = new List<string>() { "Ultra", "High", "Medium", "Low" };
        Instance.textureQualityChoose.index = TextureQuality;
        /*Instance.textureResolutionDropdown.ClearOptions();
        Instance.textureResolutionDropdown.onValueChanged.RemoveAllListeners();
        var options = new List<TMP_Dropdown.OptionData>();

        for(int i = 0; i < 4; i++) {
            string text = "";
            switch(i) {
                case 0:
                    text = "Ultra";
                    break;
                case 1:
                    text = "High";
                    break;
                case 2:
                    text = "Medium";
                    break;
                case 3:
                    text = "Low";
                    break;
            }
            var item = new TMP_Dropdown.OptionData() {
                text = text,
            };
            options.Add(item);
            Instance.textureQualityChoose.objs.Add(text);
        }*/
        /*Instance.textureResolutionDropdown.AddOptions(options);
        Instance.textureResolutionDropdown.value = (int)TextureQuality;
        Instance.textureResolutionDropdown.RefreshShownValue();
        Instance.textureResolutionDropdown.onValueChanged.AddListener((int value) => {
            SetTextureResolution(value);
            RefreshUI();
        });*/
    }
    public static void RenderShadowResolutionDropdown() {
        Instance.shadowQualityChoose.objs = new List<string>() { "Ultra", "High", "Medium", "Low" };
        Instance.shadowQualityChoose.index = ShadowQuality;
        /*Instance.shadowQualityDropdown.ClearOptions();
        Instance.shadowQualityDropdown.onValueChanged.RemoveAllListeners();
        var options = new List<TMP_Dropdown.OptionData>();

        for(int i = 0; i < 4; i++) {
            string text = "";
            switch(i) {
                case 0:
                    text = "Ultra";
                    break;
                case 1:
                    text = "High";
                    break;
                case 2:
                    text = "Medium";
                    break;
                case 3:
                    text = "Low";
                    break;
            }
            var item = new TMP_Dropdown.OptionData() {
                text = text,
            };
            options.Add(item);
        }
        Instance.shadowQualityDropdown.AddOptions(options);
        Instance.shadowQualityDropdown.value = ShadowQuality;
        Instance.shadowQualityDropdown.RefreshShownValue();
        Instance.shadowQualityDropdown.onValueChanged.AddListener((int value) => {
            SetShadowResolution(value);
            RefreshUI();
        });*/
    }

    public static void OpenOptionsMenu() {
        RefreshUI();
        OnGraphicSettingsOpen.Invoke();
	}
    public static void CloseOptionsMenu() {
        Instance.menu.TransitionMenu(1);
    }
    public static void RefreshUI() {
        RenderResolutionDropdown();
        RenderWindowModeDropdown();
        RenderTextureResolutionsDropdown();
        RenderShadowResolutionDropdown();
        
        Instance.antialiasingToggle.isOn = Antialiasing;
        Instance.ssrToggle.isOn = SSR;
        Instance.vsyncToggle.isOn = VSYNC;
        Instance.uiScaleSlider.SetValue(UIScale);
        Instance.shadowQualityChoose.index = ShadowQuality;
        Instance.textureQualityChoose.index = TextureQuality;
    }
    public static void ApplySettings() {
        SetMainVolume(MainVolume);
        SetMusicVolume(MusicVolume);
        SetGameWindowSize(GetScreenResolution(WindowGameSize), FullscreenMode);
        SetAntialiasing(Antialiasing);
        SetSSR(SSR);
        SetVsync(VSYNC);
        SetTextureResolution(TextureQuality);
        SetShadowResolution(ShadowQuality);
        SetUIScale(UIScale);
	}
    // Logics
    static IniData LoadedSettings;
    static string AudioSection => "Audio";
    static string WindowSettings => "WindowSettings";
    static string Graphics => "Graphics";
    public static int MainVolume {
        get => ParseInt(GetValue(AudioSection, nameof(MainVolume)));
        set => SetValue(AudioSection, nameof(MainVolume), value);
	}
    public static int MusicVolume {
        get => ParseInt(GetValue(AudioSection, nameof(MusicVolume)));
        set => SetValue(AudioSection, nameof(MusicVolume), value);
    }
    public static int SoundEffectVolume {
        get => ParseInt(GetValue(AudioSection, nameof(SoundEffectVolume)));
        set => SetValue(AudioSection, nameof(SoundEffectVolume), value);
    }
    static bool Antialiasing {
        get => ParseBool(GetValue(Graphics, nameof(Antialiasing)));
        set => SetValue(Graphics, nameof(Antialiasing), value);
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

    public static void Initialize() {
        Instance = FindObjectOfType<GraphicSettings>();

        if(File.Exists(GamePaths.UserGraphicSettings) == false) {
            // Create new User-Settings
            LoadedSettings = new IniData();
            VerifyGraphicSettings();
        } else {
            LoadSettings();
		}

        if(Instance == null) {
            //throw new GraphicSettingsNotFound("There is no instance found of Graphic Settings!");
        } else {
            DontDestroyOnLoad(Instance);
            Instance.menu.Initialize();
        }
    }

    // Apply Settings
    static void SetMainVolume(int volume) {
        MainVolume = volume;
	}
    static void SetMusicVolume(int volume) {
        MusicVolume = volume;
	}
    static void SetGameWindowSize(SupportedScreenSizes size, FullScreenMode mode) => SetGameWindowSize(GetScreenResolution(size), mode);
    static void SetGameWindowSize(Int2 resolution, FullScreenMode mode) {
        WindowGameSize = GetSupportedScreenSize(resolution);
        Screen.SetResolution(resolution.x, resolution.y, mode);
    }
    static void SetGameWindowMode(FullScreenMode mode) {
        FullscreenMode = mode;
        Screen.fullScreenMode = mode;
	}
    static void SetAntialiasing(bool? state) {
        Antialiasing = (bool)state;
        if((bool)state) {
            Camera.main.GetComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
        } else {
            Camera.main.GetComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.None;

        }
	}
    static void SetSSR(bool state) {
        SSR = state;
        if(Instance != null) {
            Instance.volumeSSR.active = state;
        }
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
        ShadowQuality = level;

        // Apply Shadow Resolution to all light sources
        foreach(var light in FindObjectsOfType<Light>()) {
            if(light.TryGetComponent(out HDAdditionalLightData lightdata)) {
                lightdata.SetShadowResolutionOverride(true);
                lightdata.SetShadowResolution(GetShadowResolution(level));
			}
		}
	}
    static void SetUIScale(float scale) {
        UIScale = scale;

        foreach(var scaler in FindObjectsOfType<CanvasScaler>()) {
            if(scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize) {
                scaler.scaleFactor = scale;
			}
		}
	}

    // User interface functions
    public void SetAntialiasingUI() => SetAntialiasing(antialiasingToggle.isOn);
    public void SetSSRUI() => SetSSR(ssrToggle.isOn);
    public void SetVsyncUI() => SetVsync(vsyncToggle.isOn);
    public void SetUIScaleUI() {
        SetUIScale(uiScaleSlider.Value);
    }
    public void SetTextureQualityUI() {
        SetTextureResolution(textureQualityChoose.index);
	}
    public void SetShadowQualityUI() {
        SetShadowResolution(shadowQualityChoose.index);
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
        if(GetValue(AudioSection, nameof(MainVolume)) is null) {
            MainVolume = 50;
        }
        if(GetValue(AudioSection, nameof(MusicVolume)) is null) {
            MusicVolume = 40;
		}
        if(GetValue(AudioSection, nameof(SoundEffectVolume)) is null) {
            SoundEffectVolume = 50;
        }
        if(GetValue(WindowSettings, nameof(WindowGameSize)) is null) {
            WindowGameSize = GetSupportedScreenSize(new Int2(Screen.currentResolution.width, Screen.currentResolution.height));
        }
        if(GetValue(WindowSettings, nameof(FullscreenMode)) is null) {
            FullscreenMode = FullScreenMode.FullScreenWindow;
		}
        if(GetValue(Graphics, nameof(Antialiasing)) is null) {
            Antialiasing = true;
		}
        if(GetValue(Graphics, nameof(SSR)) is null) {
            SSR = true;
		}
        if(GetValue(Graphics, nameof(VSYNC)) is null) {
            VSYNC = true;
		}
        if(GetValue(Graphics, nameof(ShadowQuality)) is null) {
            ShadowQuality = 2;
		}
        if(GetValue(Graphics, nameof(UIScale)) is null) {
            UIScale = 1;
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
            throw new GraphicSettingsNotFound("Failed to read INT value: " + value);
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
            throw new GraphicSettingsNotFound("Failed to read BOOLEAN value: " + value);
        }
    }
    static void SetValue(string section, string parameter, object value) {
        try {
            LoadedSettings[section][parameter] = value.ToString();
            SaveSettings();
		} catch {
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
        Debug.Log("No supported resolution has been found. Returning default resolution FullHD");
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
        }
        return 2048;
	}
    public static int GetShadowResolution() => GetShadowResolution(ShadowQuality);
}