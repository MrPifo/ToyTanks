using SimpleMan.Extensions;
using Sperlich.FSM;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class DebugTank : TankAI {

	public Transform moveTarget;

	public override void InitializeTank() {
		base.InitializeTank();
		if(moveTarget == null) {
			moveTarget = new GameObject().transform;
			moveTarget.name = "DebugTank_MoveTarget";
			moveTarget.position = Pos;
		}
		ProcessState(TankState.Move);
	}

	protected override IEnumerator IMove() {
		FindPath(moveTarget.position);
		Vector3 startTarget = moveTarget.position;
		while(IsPlayReady) {
			ConsumePath();
			MoveAlongPath();
			KeepHeadRot();
			if(startTarget != moveTarget.position) {
				break;
			}
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
		}
		GoToNextState();
	}

	protected override void DrawDebug() {
		base.DrawDebug();
		Game.ActiveGrid.PaintCellAt(currentDestination, Color.yellow);
		Game.ActiveGrid.PaintCellAt(nextMoveTarget, Color.green);
	}

	public void RefreshPathDebug() {
		StopAllCoroutines();
		ProcessState(TankState.Move);
	}
}
#if UNITY_EDITOR
[CustomEditor(typeof(DebugTank))]
class LevelEditorEditor : Editor {

	public string levelId;

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		var builder = (DebugTank)target;
		if(GUILayout.Button("Refresh Path")) {
			builder.RefreshPathDebug();
		}
	}
}
#endif