using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARHandler : MonoBehaviour {

    public GameObject ARTracker;
    public GameObject ARCamera;

	// Use this for initialization
	void Start () {
        transform.rotation *= Quaternion.Euler(0, 0, 0);
    }

    // Update is called once per frame
    void Update() {
       if (ARCamera.activeSelf)
       {
            transform.position = ARTracker.transform.position;// + new Vector3(0f, 0f, 0f);//new Vector3(0f, -0.0025f, 0f);
            Vector3 rot = Vector3.zero;
            rot.y = ARTracker.transform.rotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(rot);
       }
	}
}
