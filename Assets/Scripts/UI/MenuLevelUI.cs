using DG.Tweening;
using Shapes;
using UnityEngine;
using TMPro;
using SimpleMan.Extensions;

namespace ToyTanks.UI {
	public class MenuLevelUI : MonoBehaviour {

		public Game.Level level;
		[ColorUsage(true, true)]
		public Color unlockColor;
		[ColorUsage(true, true)]
		public Color lockedColor;
		[ColorUsage(true, true)]
		public Color hoverColor;
		public float fillSpeed;
		public SpriteMask mask;
		public TextMeshProUGUI levelNumber;
		public Rectangle rect;
		public Line connectLine;
		public SpriteRenderer preview;
		public GameObject lockIcon;
		public ButtonAudio.ButtonAudios hoverAudio;
		public ButtonAudio.ButtonAudios clickAudio;
		bool isUnlocked;
		Vector3 lineEndPos;

		void Awake() {
			mask.alphaCutoff = Random.Range(0.05f, 0.001f);
			mask.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, Random.Range(0, 360)));
			lineEndPos = connectLine.End;
			connectLine.End = Vector3.zero;
		}

		public void LockLevel() {
			rect.Color = lockedColor;
			isUnlocked = false;
			connectLine.End = Vector3.zero;
			lockIcon.Show();
		}

		public void UnlockLevel() {
			rect.Color = unlockColor;
			isUnlocked = true;
			lockIcon.Hide();
		}

		public void Initialize(Game.Level level) {
			this.level = level;
			try {
				var sprite = Resources.Load<Sprite>(Game.LevelScreenshotPath + level.LevelId);
				preview.sprite = sprite;
			} catch {
			}

			levelNumber.text = level == null ? "" : level.LevelId.ToString();
			if(level != null && isUnlocked) {
				float speed = (1 + level.Order) * fillSpeed;
				this.Delay(speed, () => {
					DOTween.To(() => connectLine.End, x => connectLine.End = x, lineEndPos, fillSpeed).SetEase(Ease.Linear);
				});
			}
		}

		public void OnMouseEnter() {
			if(isUnlocked) {
				rect.Color = hoverColor;
				transform.DOScale(1.4f, 0.2f).SetEase(Ease.OutCubic);
				Game.SetCursor("pointer");
				AudioPlayer.Play(hoverAudio.ToString(), AudioType.UI);
			}
		}

		public void OnMouseExit() {
			if(isUnlocked) {
				rect.Color = unlockColor;
				transform.DOScale(1.35f, 0.2f).SetEase(Ease.OutCubic);
				Game.SetCursor("default");
			}
		}

		public void OnMouseDown() {
			if(isUnlocked) {
				MenuManager.Instance.worldOverviewMenu.FadeOut();
				GameManager.StartLevel(level.LevelId);
				AudioPlayer.Play(clickAudio.ToString(), AudioType.UI);
			}
		}
	}
}