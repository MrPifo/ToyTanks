using ToyTanks.LevelEditor;

namespace ToyTanks.LevelEditor {
	public class TankUISelect : SelectItem {

		public TankAsset tankAsset;

		public void Apply(TankAsset asset) {
			tankAsset = asset;
			SetSprite(asset.preview);
		}

		public override void Select() {
			base.Select();
			LevelEditor.CurrentTank = tankAsset;
		}

		public override void Deselect() {
			base.Deselect();
		}
	}
}