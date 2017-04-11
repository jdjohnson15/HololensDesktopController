using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class remoteScaler : MonoBehaviour {

	// Use this for initialization
	void Start () {
        RemoteControl.instance.AddParam(this.transform.root.name + " Particle scale:", this.transform.localScale.x, num => { this.transform.localScale = Vector3.one * (float)num; }, .005, 1.5, .001);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
