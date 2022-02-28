using System.Collections;
using System.Collections.Generic;
using ToyTanks.LevelEditor;
using UnityEngine;

public class GroundTileExtra : GameEntity {
    
    public GroundTileType tileType;
    public MeshRenderer meshRender;
    public List<ExtraPrefabTheme> extraPrefabThemes;


    public virtual void SetTheme(WorldTheme theme) {
        meshRender.material = extraPrefabThemes.Find(t => t.theme == theme).material;
    }

    [System.Serializable]
    public class ExtraPrefabTheme {
        public Material material;
        public WorldTheme theme;
    }
}
