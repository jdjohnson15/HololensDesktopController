using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Globals : MonoBehaviour {

	public enum Mode { MODE_SETUP, MODE_ACTIVE, MODE_DEBUG};

    public Mode mode;

    public GameObject[] plinths;

    void Start()
    {
        mode = Mode.MODE_SETUP;

        RemoteControl.instance.AddParam("ACTIVE MODE: ", false, unused => {
            mode = Mode.MODE_ACTIVE;
        });
        RemoteControl.instance.AddParam("SETUP MODE: ", false, unused => {
            mode = Mode.MODE_SETUP;
        });
        RemoteControl.instance.AddParam("DEBUG MODE: ", false, unused => {
            mode = Mode.MODE_DEBUG;
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (mode == Mode.MODE_ACTIVE)
                mode = Mode.MODE_SETUP;
            else
                mode = Mode.MODE_ACTIVE;
        }
    }

    public void changeModeAlpha()
    {
        mode = Mode.MODE_ACTIVE;
    }

    public void changeModeBeta()
    {
        mode = Mode.MODE_SETUP;
    }
}
