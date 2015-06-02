﻿using UnityEngine;
using System.Collections;

public class ReferencePoint : MonoBehaviour {
    void Start() {
        Debug.Log(this.GetPath() + " - " + transform.position);

        Destroy(gameObject);
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, Vector3.one * 0.1f);
        Gizmos.DrawLine(transform.position + new Vector3(-10, 0, 0), transform.position + new Vector3(10, 0, 0));
        Gizmos.DrawLine(transform.position + new Vector3(0, -10, 0), transform.position + new Vector3(0, 10, 0));
        Gizmos.DrawLine(transform.position + new Vector3(0, 0, -10), transform.position + new Vector3(0, 0, 10));
        Gizmos.color = Color.white;
    }
}
