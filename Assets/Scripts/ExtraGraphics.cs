using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;


/// <summary>
/// Enables getting/setting URP graphics settings properties that don't have built-in getters and setters.
/// </summary>
public static class ExtraGraphics {
    private static FieldInfo MainLightCastShadows_FieldInfo;
    private static FieldInfo AdditionalLightCastShadows_FieldInfo;
    private static FieldInfo MainLightShadowmapResolution_FieldInfo;
    private static FieldInfo AdditionalLightShadowmapResolution_FieldInfo;
    private static FieldInfo Cascade2Split_FieldInfo;
    private static FieldInfo Cascade4Split_FieldInfo;
    private static FieldInfo SoftShadowsEnabled_FieldInfo;
    private static FieldInfo AOSampleCount_FieldInfo;
    private static FieldInfo AOQuality_FieldInfo;
    private static FieldInfo AOSettings_FieldInfo;
    private static FieldInfo DecalSettings_FieldInfo;
    private static FieldInfo AODownsample_FieldInfo;
    private static FieldInfo AOAfterOpaque_FieldInfo;
    private static FieldInfo DecalDrawDistance_FieldInfo;

    static ExtraGraphics() {
        //ScreenSpaceAmbientOcclusion
        var pipelineAssetType = typeof(UniversalRenderPipelineAsset);
        var aoType = System.Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusionSettings, Unity.RenderPipelines.Universal.Runtime", true, true);
        var aoSettings = System.Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime", true, true);
        var decalType = System.Type.GetType("UnityEngine.Rendering.Universal.DecalRendererFeature, Unity.RenderPipelines.Universal.Runtime", true, true);
        var decalSettings = System.Type.GetType("UnityEngine.Rendering.Universal.DecalSettings, Unity.RenderPipelines.Universal.Runtime", true, true);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        MainLightCastShadows_FieldInfo = pipelineAssetType.GetField("m_MainLightShadowsSupported", flags);
        AdditionalLightCastShadows_FieldInfo = pipelineAssetType.GetField("m_AdditionalLightShadowsSupported", flags);
        MainLightShadowmapResolution_FieldInfo = pipelineAssetType.GetField("m_MainLightShadowmapResolution", flags);
        AdditionalLightShadowmapResolution_FieldInfo = pipelineAssetType.GetField("m_AdditionalLightsShadowmapResolution", flags);
        Cascade2Split_FieldInfo = pipelineAssetType.GetField("m_Cascade2Split", flags);
        Cascade4Split_FieldInfo = pipelineAssetType.GetField("m_Cascade4Split", flags);
        SoftShadowsEnabled_FieldInfo = pipelineAssetType.GetField("m_SoftShadowsSupported", flags);

        AOSettings_FieldInfo = aoSettings.GetField("m_Settings", flags);
        AOSampleCount_FieldInfo = aoType.GetField("SampleCount", flags);
        AOQuality_FieldInfo = aoType.GetField("NormalSamples", flags);
        AODownsample_FieldInfo = aoType.GetField("Downsample", flags);
        AOAfterOpaque_FieldInfo = aoType.GetField("AfterOpaque", flags);

        DecalSettings_FieldInfo = decalSettings.GetField("m_Settings", flags);
        DecalDrawDistance_FieldInfo = decalType.GetField("maxDrawDistance", flags);
    }

    public static int AO_SampleRate {
        get {
            object field = AOSettings_FieldInfo.GetValue(GraphicSettings.GetAORendererFeature());
            return (int)AOSampleCount_FieldInfo.GetValue(field);
		}
        set {
            object field = AOSettings_FieldInfo.GetValue(GraphicSettings.GetAORendererFeature());
            AOSampleCount_FieldInfo.SetValue(field, value);
        }
	}

    public static int AO_Quality {
        get {
            object field = AOSettings_FieldInfo.GetValue(GraphicSettings.GetAORendererFeature());
            return (int)AOQuality_FieldInfo.GetValue(field);
        }
        set {
            object field = AOSettings_FieldInfo.GetValue(GraphicSettings.GetAORendererFeature());
            AOQuality_FieldInfo.SetValue(field, value);
        }
    }

    public static bool AO_Downsample {
        get {
            object field = AOSettings_FieldInfo.GetValue(GraphicSettings.GetAORendererFeature());
            return (bool)AODownsample_FieldInfo.GetValue(field);
        }
        set {
            object field = AOSettings_FieldInfo.GetValue(GraphicSettings.GetAORendererFeature());
            AODownsample_FieldInfo.SetValue(field, value);
        }
    }

    public static float Decal_DrawDistance {
        get {
            object field = DecalSettings_FieldInfo.GetValue(GraphicSettings.GetDecalRendererFeature());
            return (float)DecalDrawDistance_FieldInfo.GetValue(field);
        }
        set {
            object field = DecalSettings_FieldInfo.GetValue(GraphicSettings.GetDecalRendererFeature());
            DecalDrawDistance_FieldInfo.SetValue(field, value);
        }
    }

    /// <summary>
    /// Improves performance
    /// </summary>
    public static bool AO_AfterOpaque {
        get {
            object field = AOSettings_FieldInfo.GetValue(GraphicSettings.GetAORendererFeature());
            return (bool)AOAfterOpaque_FieldInfo.GetValue(field);
        }
        set {
            object field = AOSettings_FieldInfo.GetValue(GraphicSettings.GetAORendererFeature());
            AOAfterOpaque_FieldInfo.SetValue(field, value);
        }
    }

    public static bool MainLightCastShadows {
        get => (bool)MainLightCastShadows_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => MainLightCastShadows_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static bool AdditionalLightCastShadows {
        get => (bool)AdditionalLightCastShadows_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => AdditionalLightCastShadows_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static ShadowResolution MainLightShadowResolution {
        get => (ShadowResolution)MainLightShadowmapResolution_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => MainLightShadowmapResolution_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static ShadowResolution AdditionalLightShadowResolution {
        get => (ShadowResolution)AdditionalLightShadowmapResolution_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => AdditionalLightShadowmapResolution_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static float Cascade2Split {
        get => (float)Cascade2Split_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => Cascade2Split_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static Vector3 Cascade4Split {
        get => (Vector3)Cascade4Split_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => Cascade4Split_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }

    public static bool SoftShadowsEnabled {
        get => (bool)SoftShadowsEnabled_FieldInfo.GetValue(GraphicsSettings.currentRenderPipeline);
        set => SoftShadowsEnabled_FieldInfo.SetValue(GraphicsSettings.currentRenderPipeline, value);
    }
}