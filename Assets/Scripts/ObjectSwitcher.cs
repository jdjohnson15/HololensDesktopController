using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSwitcher : MonoBehaviour {
    Globals globals;
    Globals.Mode oldMode;
	// Use this for initialization
	void Start () {
        globals = GameObject.Find("Globals").GetComponent<Globals>();
        oldMode = Globals.Mode.MODE_ACTIVE;
	}
	
	// Update is called once per frame
	void Update () {
        if (globals.mode!= oldMode)
        {
            if (globals.mode == Globals.Mode.MODE_ACTIVE)
            {
                this.transform.GetChild(0).gameObject.SetActive(false);
                this.transform.GetChild(1).gameObject.SetActive(true);
            }
            if (globals.mode == Globals.Mode.MODE_SETUP)
            {
                this.transform.GetChild(0).gameObject.SetActive(true);
                this.transform.GetChild(1).gameObject.SetActive(false);
            }
        }
        oldMode = globals.mode;
		
    }
}
