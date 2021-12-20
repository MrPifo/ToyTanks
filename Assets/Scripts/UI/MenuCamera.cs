using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MenuCamera : MonoBehaviour {

    public Camera camera;
    public float debugSpeed;
    public float wiggleAmount;
    public float smoothSpeed;
    public float refreshSpeed;
    Vector3 origin;
    Vector3 target;
    float time;
    bool moveUp;
    bool moveRight;
    bool moveLeft;
    bool moveDown;
    bool perspective;
    public bool WiggleActive { get; set; }

    void Awake() {
        WiggleActive = true;
        origin = transform.position;
    }

	void LateUpdate() {
        //Wiggle();

        if(moveLeft) {
            transform.position += new Vector3(-1, 0, 0) * debugSpeed * Time.deltaTime;
        }
        if(moveRight) {
            transform.position += new Vector3(1, 0, 0) * debugSpeed * Time.deltaTime;
        }
        if(moveDown) {
            transform.position += new Vector3(0, 0, -1) * debugSpeed * Time.deltaTime;
        }
        if(moveUp) {
            transform.position += new Vector3(0, 0, 1) * debugSpeed * Time.deltaTime;
        }
    }

    public void MoveLeft() {
        moveLeft = !moveLeft;
    }
    public void MoveRight() {
        moveRight = !moveRight;
    }
    public void MoveForward() {
        moveUp = !moveUp;
    }
    public void MoveBackward() {
        moveDown = !moveDown;
    }
    public void ZoomIn() {
        camera.orthographicSize += 0.2f;
	}
    public void ZoomOut() {
        camera.orthographicSize -= 0.2f;
    }
    public void SwitchPerspective() {
        if(perspective) {
            camera.orthographic = false;
            perspective = false;
		} else {
            camera.orthographic = true;
            perspective = true;
		}
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
