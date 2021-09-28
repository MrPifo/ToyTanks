using UnityEngine.UI;

namespace ToyTanks.LevelEditor {
    public class LevelBlockUI : SelectItem {

		public ThemeAsset theme;
		public LevelEditor.BlockTypes blockType;
		public Image buttonImage;

		public void Apply(ThemeAsset theme, LevelEditor.BlockTypes type) {
			this.theme = theme;
			blockType = type;
			SetSprite(theme.GetAsset(type)?.preview);
		}

		public override void Select() {
			base.Select();
			LevelEditor.CurrentAsset = theme.GetAsset(blockType);
		}

		public override void Deselect() {
			base.Deselect();
		}
	}
}