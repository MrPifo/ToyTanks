using EpPathFinding.cs;
using System.Collections.Generic;
using UnityEngine;
using SimpleMan.Extensions;
// HDRP Related: using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using DG.Tweening;

public static class ExtensionMethods {

    public static float Remap(this float from, float fromMin, float fromMax, float toMin, float toMax) {
        var fromAbs = from - fromMin;
        var fromMaxAbs = fromMax - fromMin;

        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;

        return to;
    }
    public static int ExtractLayer(this LayerMask layerMask) {
        int layerNumber = 0;
        int layer = layerMask.value;
        while(layer > 0) {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber - 1;
    }
    public static Vector3 GetWorldPos(this BaseGrid grid, GridPos pos) => GetWorldPos(grid, pos.x, pos.y);
    public static Vector3 GetWorldPos(this BaseGrid grid, int indexX, int indexY) {
        float x = grid.WorldPos.x + indexX * grid.CellSize;
        float y = grid.WorldPos.z + indexY * grid.CellSize;
        return new Vector3(x, 0, y);
    }
    public static GridPos GetGridPos(this StaticGrid grid, Vector3 pos) {
        // Use this instead for precise rounding and consistent grid positions
        int x = (int)System.Math.Round((pos.x - grid.WorldPos.x) / grid.CellSize, 0, System.MidpointRounding.AwayFromZero);
        int z = (int)System.Math.Round((pos.z - grid.WorldPos.z) / grid.CellSize, 0, System.MidpointRounding.AwayFromZero);

        return new GridPos(x, z);
    }
    public static int Layer(this RaycastHit hit) => hit.transform.gameObject.layer;
    public static GridPos GetGridPosSmart(this StaticGrid grid, Vector3 pos, bool mirrorSearch = false) {
        // Use this instead for precise rounding and consistent grid positions
        int x = (int)System.Math.Round((pos.x - grid.WorldPos.x) / grid.CellSize);
        int z = (int)System.Math.Round((pos.z - grid.WorldPos.z) / grid.CellSize);

        if(grid.HasIndex(x, z) == false || grid.IsWalkableAt(x, z) == false) {
            return GetNearestValid(grid, new GridPos(x, z), mirrorSearch);
        } else {
            return new GridPos(x, z);
        }
    }
    public static GridPos GetNearestValid(this StaticGrid grid, GridPos index, bool mirrorSearch = false) {
        int x = 0;
        int y = 0;
        // Looking for nearest valid tile with a spiral pattern
        for(int i = 0; i < grid.width * grid.height; ++i) {
            if(System.Math.Abs(x) <= System.Math.Abs(y) && (x != y || x >= 0)) {
                if(mirrorSearch) {
                    x += ((y >= 0) ? -1 : 1);
                } else {
                    x += ((y >= 0) ? 1 : -1);
                }
            } else {
                if(mirrorSearch) {
                    y += ((x >= 0) ? 1 : -1);
                } else {
                    y += ((x >= 0) ? -1 : 1);
                }
            }

            GridPos tPos = new GridPos(index.x + x, index.y + y);
            //Game.ActiveGrid.PaintCellAt(Game.ActiveGrid.Grid.GetWorldPos(tPos), Color.magenta, 0.5f);
            if(grid.HasIndex(tPos) && grid.IsWalkableAt(tPos)) {
                return tPos;
            }
        }

        return null;
    }
    /// <summary>
    /// Searches for a component on the given Transform. Search order goes by: Transform itself -> Transforms Children -> Transforms Parent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public static T SearchComponent<T>(this Transform t) {
        T comp = t.GetComponent<T>();
        if(comp == null) {
            comp = t.GetComponentInChildren<T>();
		}
        if(comp == null) {
            comp = t.GetComponentInParent<T>();
		}
        return comp;
	}
    public static bool TrySearchComponent<T>(this Transform t, out T component) {
        component = t.SearchComponent<T>();
        if(component == null) {
            return false;
		}
        return true;
	}
	public static GameObject FindChild(this MonoBehaviour mono, string name) => mono.gameObject.FindChild(name);
	public static GameObject FindChild(this Transform transform, string name) => transform.gameObject.FindChild(name);
	public static GameObject FindChild(this GameObject gameobject, string name) {
		name = name.ToLower();
		GameObject result = null;
		foreach(Transform t in gameobject.transform) {
			if(t.name.ToLower() == name) {
				result = t.gameObject;
			}
		}
		if(result == null) {
			Debug.LogWarning($"Unable to find {name}");
		}
		return result;
	}
	public static T RandomItem<T>(this List<T> list) {
        return list[Randomizer.Range(0, list.Count - 1)];
    }
	public static T RandomItem<T>(this List<T> list, int max) {
		return list[Randomizer.Range(0, max - 1)];
	}
    /* HDRP Related: 
    public static void FadeIntensity(this HDAdditionalLightData lightData, float startValue, float endValue, float duration) {
		DestructionTimer destructor = new GameObject("lightFadeIntensity_" + lightData.name).AddComponent<DestructionTimer>();
		destructor.Destruct(duration);
		destructor.StartCoroutine(IFade());
		IEnumerator IFade() {
			float time = 0;
			while(time < duration) {
				lightData.SetIntensity(time.Remap(0f, 1f, startValue, endValue));
				time += Time.deltaTime;
				yield return null;
			}
		}
	}*/
	public static void Show(this MonoBehaviour mono) {
		mono.gameObject.SetActive(true);
	}
	public static void Hide(this MonoBehaviour mono) {
		mono.gameObject.SetActive(false);
	}
	public static void Show(this GameObject gameobject) {
		gameobject.gameObject.SetActive(true);
	}
	public static void Hide(this GameObject gameobject) {
		gameobject.gameObject.SetActive(false);
	}
	public static void Show(this Transform transform) {
		transform.gameObject.SetActive(true);
	}
	public static void Hide(this Transform transform) {
		transform.gameObject.SetActive(false);
	}
	/// <summary>
	/// Destroys this GameObject
	/// </summary>
	/// <param name="gameObject"></param>
	//public static void Destroy(this GameObject gameObject) => Object.Destroy(gameObject);
	/// <summary>
	/// Destroy the holding GameObject
	/// </summary>
	/// <param name="transform"></param>
	//public static void Destroy(this Transform transform) => Object.Destroy(transform.gameObject);
}
