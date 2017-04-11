using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OSCEvents : MonoBehaviour {

    public int ID;
    public GameObject _OSCPlinthManager;
    public GameObject[] light; //this should be handled elsewhere, but for now it is here. 

    //public GameObject _OSCControllerManager; no longer needed, but keep just in case

    void Start()
    {
        
    }

    public void OnGaze()
    {
        /*if (_OSCControllerManager != null)
        {
            _OSCControllerManager.GetComponent<OSCControlManager>().setGazedID(ID);
        }
        else
        {
            Debug.Log("OSC Controller Interface not initiated!");
        }*/

        if (_OSCPlinthManager != null)
        {
            _OSCPlinthManager.GetComponent<OSCPlinthManager>().setArg(ID, 0, 1f);
        }
        else
        {
            Debug.Log("OSC Plinth Interface not initiated!");
        }

        if (light != null)
        {
            foreach(GameObject l in light)
                l.GetComponent<LightController>().setActivate(true);    
        }
        else
        {
            Debug.Log("No light connected");
        }
    }

    public void OnUngaze()
    {
        _OSCPlinthManager.GetComponent<OSCPlinthManager>().setArg(ID, 0, 0f);
        if (light != null)
        {
            foreach (GameObject l in light)
                l.GetComponent<LightController>().setActivate(false);

        }
            
        else
        {
            Debug.Log("No light connected");
        }
    }

    public void OnStory()
    {
       _OSCPlinthManager.GetComponent<OSCPlinthManager>().setArg(ID, 1, 1f);
    }

    public void OnUnstory()
    {
       _OSCPlinthManager.GetComponent<OSCPlinthManager>().setArg(ID, 1, 0f);
    }
}
