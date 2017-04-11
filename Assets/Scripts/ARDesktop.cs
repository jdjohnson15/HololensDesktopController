using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARDesktop : MonoBehaviour {

    public Vector2 screenSize;
    public GameObject OSCManager;

    private GameObject debugPointer;

    private RaycastHit hitInfo;

    private bool OSCDefined;

    private int layerMask;

    private Bounds bounds;

    private float tapped = 0f;
	// Use this for initialization
	void Start () {
        layerMask = 1 << 2;
        layerMask = ~layerMask;

        if (OSCManager.GetComponent<OSCARDesktopManager>())
        {
            OSCDefined = true;
        }
        else
        {
            OSCDefined = false;
        }

        OSCDefined = true; //debug purposes 

        if (OSCDefined)
        {
            bounds = this.gameObject.GetComponent<Collider>().bounds;
        }

        debugPointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugPointer.transform.localScale = Vector3.one * 0.005f;
        debugPointer.GetComponent<MeshRenderer>().material.color = Color.blue;
    }

    // Update is called once per frame
    void Update()
    {
        if (OSCDefined)
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, Mathf.Infinity, layerMask))
            {
                if (hitInfo.collider.gameObject == this.gameObject)
                {
                    debugPointer.transform.position = hitInfo.point;
                    // OSCManager.GetComponent<OSCARDesktopManager>().setArg(calculateDesktopCoordinates(hitInfo.point), tapped);
                    calculateDesktopCoordinates(hitInfo.point);
                    if (tapped == 1f)
                        tapped = 0f;
                }
                
            }
        }
            

    }

    Vector2 calculateDesktopCoordinates(Vector3 hitPoint)
    {
        Vector3 relativePoint = Quaternion.Inverse(this.transform.rotation)*(hitPoint - this.transform.position);

        //Vector2 outputPoint = new Vector2((relativePoint.x/bounds.size.x+1f)/2f*screenSize.x,(relativePoint.y/bounds.size.y+1)/2f*screenSize.y);
        Vector2 outputPoint = new Vector2((relativePoint.x / bounds.size.x +.5f)* screenSize.x, (relativePoint.y / bounds.size.y+.5f)* screenSize.y);
        Debug.Log("screen pos: " + outputPoint);
        return outputPoint;
    }

    void OnTapped()
    {
        tapped = 1f;
    }
}
