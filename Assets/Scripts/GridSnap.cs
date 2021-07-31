using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class GridSnap : MonoBehaviour
{
    public bool snap = true;

    void Awake()
    {
        if (Application.isPlaying)
        {
            snap = false;
        }
    }
    public void Update()
    {
        if (snap)
        {
            Vector3 pos = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), Mathf.Round(transform.position.z));
            transform.position = pos;
        }
    }
}
