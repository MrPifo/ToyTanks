using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MenuCamera : MonoBehaviour {

    public float wiggleAmount;
    public float smoothSpeed;
    public float refreshSpeed;
    Vector3 origin;
    Vector3 target;
    float time;
    public bool WiggleActive { get; set; }

    void Awake() {
        WiggleActive = true;
        origin = transform.position;
    }

	void LateUpdate() {
        Wiggle();
    }

    public void Wiggle() {
        if(WiggleActive) {
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * smoothSpeed);
            time += Time.deltaTime;
            if(time > refreshSpeed) {
                time = 0;
                DOTween.To(x => smoothSpeed = x, smoothSpeed / 2f, smoothSpeed, refreshSpeed);
                target = origin + new Vector3(Random.Range(-wiggleAmount, wiggleAmount), 0, Random.Range(-wiggleAmount, wiggleAmount));
            }
        }
    }

    public void UpdateWiggle() {
        origin = transform.position;
	}
}
