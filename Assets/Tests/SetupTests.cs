using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TestSetup {
    
    [Test]
    public void TestTags() {
        Assert.DoesNotThrow(() => new GameObject() { tag = "Player"});
        Assert.DoesNotThrow(() => new GameObject() { tag = "Bot" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "MenuItem" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "TransitionFlashScreen" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "Destructable" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "ShadowQualityUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "TextureQualityUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "AntialisingQualityUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "MainVolumeUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "SoundEffectsVolumeUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "AmbientOcclusionQualityUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "PerformanceModeUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "ShootTouchRegion" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "AimJoystick" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "TouchPad" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "ControlSchemeUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "WindowModeUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "WindowResolutionUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "VsyncUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "LevelPreset" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "GroundTile" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "GroundTileExtra" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "BulletBlocker" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "BulletReflector" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "FidelityFXUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "OutlineUI" });
        Assert.DoesNotThrow(() => new GameObject() { tag = "Ground" });
    }

    [Test]
    public void TestLayers() {
        Assert.AreEqual(LayerMask.NameToLayer("Bullet"), 6);
        Assert.AreEqual(LayerMask.NameToLayer("Player"), 7);
        Assert.AreEqual(LayerMask.NameToLayer("Ground"), 8);
        Assert.AreEqual(LayerMask.NameToLayer("Bot"), 9);
        Assert.AreEqual(LayerMask.NameToLayer("Destructable"), 10);
        Assert.AreEqual(LayerMask.NameToLayer("DestructionPieces"), 11);
        Assert.AreEqual(LayerMask.NameToLayer("Blur"), 12);
        Assert.AreEqual(LayerMask.NameToLayer("LoadingScreen"), 13);
        Assert.AreEqual(LayerMask.NameToLayer("BulletReject"), 14);
        Assert.AreEqual(LayerMask.NameToLayer("LevelBoundary"), 15);
        Assert.AreEqual(LayerMask.NameToLayer("BulletTraverse"), 16);
        Assert.AreEqual(LayerMask.NameToLayer("IgnoreSeeThrough"), 17);
        Assert.AreEqual(LayerMask.NameToLayer("Block"), 18);
        Assert.AreEqual(LayerMask.NameToLayer("CrossHair"), 19);
        Assert.AreEqual(LayerMask.NameToLayer("Hole"), 22);
        Assert.AreEqual(LayerMask.NameToLayer("BulletBlocker"), 24);
        Assert.AreEqual(LayerMask.NameToLayer("Foliage"), 25);
    }
}
