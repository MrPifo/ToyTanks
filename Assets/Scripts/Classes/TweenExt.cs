using System;
using DG.Tweening;
using UnityEngine;

public static class TweenExt {

	public static void Pulse(this Transform t, float duration, float maxSize) {
		Vector3 startSize = t.localScale;
		var seq = DOTween.Sequence();
		seq.Append(t.DOScale(startSize * maxSize, duration / 2f)).Append(t.DOScale(startSize, duration / 2f));
		seq.Play();
	}

}
