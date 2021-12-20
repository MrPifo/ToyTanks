using DG.Tweening;
using Shapes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

namespace ToyTanks.UI {
	public class MenuLevelUI : MonoBehaviour {

		Game.Level level;
		[ColorUsage(true, true)]
		public Color unlockColor;
		[ColorUsage(true, true)]
		public Color lockedColor;
		[ColorUsage(true, true)]
		public Color hoverColor;
		public SpriteMask mask;
		public TextMeshProUGUI levelNumber;
		public Rectangle rect;
		public SpriteRenderer preview;
		List<(ShapeRenderer shape, Vector3 lineEnd, float angleStart, float angleEnd)> shapes;
		public int elements => shapes.Count;
		float originalThickness;
		bool isUnlocked;
		bool isSelected;

		void Awake() {
			originalThickness = rect.Thickness;
			mask.alphaCutoff = Random.Range(0.05f, 0.001f);
			mask.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, Random.Range(0, 360)));
		}

		public void LockLevel() {
			rect.Color = lockedColor;
			rect.Thickness = 1;
		}

		public void UnlockLevel() {
			rect.Color = unlockColor;
			rect.Thickness = originalThickness;
		}

		public void Initialize(Game.Level level, bool isUnlocked) {
			this.level = level;
			this.isUnlocked = isUnlocked;
			try {
				var sprite = Resources.Load<Sprite>(Game.LevelScreenshotPath + level.LevelId);
				preview.sprite = sprite;
			} catch { }
			levelNumber.text = level == null ? "" : level.LevelId.ToString();
			shapes = new List<(ShapeRenderer shape, Vector3 lineEnd, float angleStart, float angleEnd)>();

			int count = 0;
			for(int i = transform.GetSiblingIndex(); i < transform.parent.childCount; i++) {
				if(i + 1 < transform.parent.childCount) {
					var next = transform.parent.GetChild(i + 1);
					if(next.TryGetComponent(out MenuLevelUI _) == false) {
						if(next.TryGetComponent(out Line l)) {
							shapes.Add((l, l.End, 0, 0));
							l.End = Vector3.zero;
						} else if(next.TryGetComponent(out Disc d)) {
							shapes.Add((d, Vector3.zero, d.AngRadiansStart, d.AngRadiansEnd));
							if((int)(d.AngRadiansEnd * Mathf.Rad2Deg) == 90) {
								d.AngRadiansStart = 90 * Mathf.Deg2Rad;
							} else {
								d.AngRadiansStart = 0;
								d.AngRadiansEnd = 0;
							}
						}
						count++;
					} else {
						break;
					}
				}
			}
		}

		public void ResetShapes() {
			foreach(var shape in shapes) {
				if(shape.shape is Line) {
					var line = shape.shape as Line;
					line.End = shape.lineEnd;
				} else if(shape.shape is Disc) {
					var ring = shape.shape as Disc;
					ring.AngRadiansStart = shape.angleStart;
					ring.AngRadiansEnd = shape.angleEnd;
				}
			}
		}

		public void FillTransition(float delay, float duration) {
			DOTween.defaultAutoPlay = AutoPlay.AutoPlayTweeners;
			var seq = DOTween.Sequence();
			seq.AppendInterval(delay);
			for(int i = 0; i < shapes.Count; i++) {
				var shape = shapes[i];
				if(shape.shape is Line) {
					var line = shapes[i].shape as Line;
					seq.Append(DOTween.To(() => line.End = Vector3.zero, x => line.End = x, shape.lineEnd, duration).SetEase(Ease.Linear));
				} else if(shape.shape is Disc) {
					var ring = shapes[i].shape as Disc;
					if((int)(shape.angleEnd * Mathf.Rad2Deg) == 90) {
						// Case if Ring is quarter circle
						seq.Append(DOTween.To(x => ring.AngRadiansStart = x, ring.AngRadiansStart, shape.angleStart, duration).SetEase(Ease.Linear));
					} else {
						// Case if Ring is half circle
						seq.Append(DOTween.To(x => ring.AngRadiansEnd = x, ring.AngRadiansEnd, shape.angleEnd, duration).SetEase(Ease.Linear));
					}
				}
			}
			seq.SetEase(Ease.Linear).Play();
		}

		public void OnMouseEnter() {
			if(isUnlocked) {
				rect.Color = hoverColor;
				isSelected = true;
				transform.DOScale(1.4f, 0.2f).SetEase(Ease.OutCubic);
				Game.SetCursor("pointer");
			}
		}

		public void OnMouseExit() {
			if(isUnlocked) {
				rect.Color = unlockColor;
				isSelected = false;
				transform.DOScale(1.35f, 0.2f).SetEase(Ease.OutCubic);
				Game.SetCursor("default");
			}
		}

		public void OnMouseDown() {
			if(isUnlocked) {
				isSelected = false;
				MenuManager.Instance.worldOverviewMenu.FadeOut();
				GameManager.StartLevel(level.LevelId);
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(MenuLevelUI))]
	public class MenuLevelUIEditor : Editor {
		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			var builder = (MenuLevelUI)target;
			if(GUILayout.Button("Animate")) {
				builder.ResetShapes();
				builder.Initialize(null, false);
				builder.FillTransition(0f, 1);
			}
		}
	}
#endif
}