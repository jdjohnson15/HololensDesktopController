using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusPoint : MonoBehaviour {

    class PlintInfo
    {
        public float distanceToCamera;
        public Vector3 position;
        public bool isVisible;
    }
    public List<GameObject> plinths;
    private List<string> visiblePlinths;
    //private List<PlintInfo> plinthInfos;

    public bool drawDebug = true;

    public bool applyHeightOffset;

    public GameObject EventManager;

    CustomEventManager emInstance;

    Vector3 focusPointTarget;

    Vector3 oldFocusPointTarget;

    string targetName;

    private GameObject debugSphere;

    public float heightOffset;

    private Plane[] frustrumPlanes;

    public bool freePlane = false;

    List<Bounds> bounds;

    // Use this for initialization
    void Start () {
        bounds = new List<Bounds>();
        visiblePlinths = new List<string>();
        foreach (GameObject plinth in plinths)
        {
            bounds.Add(plinth.GetComponent<Collider>().bounds);
        }
        if (drawDebug)
        {
            debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphere.GetComponent<Renderer>().material.color = Color.cyan;
            debugSphere.transform.localScale = new Vector3(.5f, .5f, .05f);//new Vector3(0.02f, 0.02f, 0.02f);
            debugSphere.layer = 2;
        }

       /* if (applyHeightOffset)
            heightOffset = bounds[0].size.y *.75f;//if all plinths are the same height
        else
            heightOffset = 0;*/

        frustrumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        
        oldFocusPointTarget = focusPointTarget = Vector3.zero;

        if (EventManager != null)
            emInstance = EventManager.GetComponent<CustomEventManager>();
        else
            emInstance = null;

    }
	
	// Update is called once per frame
	void Update () {

        //calc direction plane is facing (towards the camera)
        Vector3 normal = -Camera.main.transform.forward;

        //check the gaze, if an EventManager instance is ready
        GameObject gazedGO = null;

        if (emInstance!= null)
        {
            gazedGO = emInstance.GazedObject;
        }
       // if (gazedGO == null)
        //{
            //visiblePlinths = new List<string>();

        //refresh the bounds(only updates when change in plinth position detected)          
        if (bounds[0].center != plinths[0].GetComponent<Collider>().bounds.center)
        {
            bounds = new List<Bounds>();
            foreach (GameObject plinth in plinths)
            {
                bounds.Add(plinth.GetComponent<Collider>().bounds);
            }
        }
            
        //update camera frustum. Used to calculate if the camera is looking at a plinth (must be done every frame because camera always moves)
        frustrumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        int index = 0;
        bool newPlinthSeen = true;
        oldFocusPointTarget = focusPointTarget;

        //get list of visibile plinths
        foreach (GameObject plinth in plinths)
        {
            //a plinth is visible if:
            // 1. the plinth's collider intersects or is inside the camera's projection frustum AND
            // 2. the camera itself is not colliding with the plinth (camera/plinth collision causes an annoying glitch, so it's best to avoid it all together)
            if (GeometryUtility.TestPlanesAABB(frustrumPlanes, bounds[index]) && Vector3.Distance(plinth.transform.position, Camera.main.transform.position)>0.3)//!plinth.GetComponent<Collider>().bounds.Contains(Camera.main.transform.position)) 
            {
                visiblePlinths.Add(plinth.name);
            }
            index++;
        }

        //plinths are seen
        if (visiblePlinths.Count > 0)
        {
            //more than one visible plinth requires extra decretion to decide which plinth gets the focus point  
            //for now the plane goes to the closest visible plinth, since plinths further away won't have holograms
            if (visiblePlinths.Count > 1)
            {         
                float smallestDist = 1000000f; //arbitrarily large number 
                index = 0;
                List<string> plinthsToBeRemoved = new List<string>();
                foreach (string plinthName in visiblePlinths)
                {
                    //Debug.Log("list  " + index+": "+visiblePlinths[index]);
                    /* if (!GeometryUtility.TestPlanesAABB(frustrumPlanes, bounds[index]))
                        {
                            plinthsToBeRemoved.Add(plinthName);
                            //Debug.Log("removed " + plinthName + " from visible list");

                        }
                        else
                        {*/
                    float d = Vector3.Distance(GameObject.Find(plinthName).transform.position, Camera.main.transform.position);
                    if (d < smallestDist)
                    {
                        smallestDist = d;
                        focusPointTarget = GameObject.Find(plinthName).transform.GetChild(0).transform.position;
                        //normal = GameObject.Find(plinthName).transform.forward;
                        // focusPointTarget.y += heightOffset;
                        targetName = plinthName;
                    }
                    //}
                    index++;
                }
                /* foreach (string plinthName in plinthsToBeRemoved)
                    {
                        visiblePlinths.Remove(plinthName);
                        Debug.Log("removed " + plinthName + " from visible list");
                    }*/

            }
            //if there's only one visible plinth, it automatically gets the focus point
            else if (visiblePlinths.Count == 1)
            {
                if (newPlinthSeen)
                {
                    focusPointTarget = GameObject.Find(visiblePlinths[0]).transform.GetChild(0).transform.position;
                    //normal = GameObject.Find(visiblePlinths[0]).transform.GetChild(0).forward;
                    targetName = visiblePlinths[0];
                    //visiblePlinths.RemoveAt(0);
                }

            }
        }
        //no plinth seen or gazed, put the stability plane a meter in front of the user
        else
        {
            focusPointTarget = Camera.main.transform.position + Camera.main.transform.forward * 1f;
            targetName = null;
        }           
        //}
        //simply put the stability frame on the gazed object if one exists, as determined by the EventManager class (placeholder method, might be better to make activation based on proximity)
       /* else
        {
            focusPointTarget = gazedGO.transform.position;
           // targetName = gazedGO.name;
        }*/

        if (targetName != null && applyHeightOffset)
        {
            focusPointTarget.y += heightOffset;
        }

        //set the focus point for the stability frame
        UnityEngine.VR.WSA.HolographicSettings.SetFocusPointForFrame(focusPointTarget, normal); //not most elegant way to handle this. Might benefit from smooth tween between new points
        if (this.GetComponent<DrawActive>())
        {
            //activate a child object listed by an instance of DrawActive declared by this object 
            this.GetComponent<DrawActive>().setActive(targetName, true);
        }
        //Debug.Log("Target is now " + targetName);
        //  }

        // Debug.Log("plane pos: " + position);
        if (drawDebug)
        {
            debugSphere.transform.position = focusPointTarget;
            debugSphere.transform.rotation = Quaternion.LookRotation(normal);
        }
        //clear lists
        for (int i = 0; i< visiblePlinths.Count; i++)
        {
           visiblePlinths.RemoveAt(i);
        }

    }
    private void DrawDebug()
    {
        if (Application.isPlaying && drawDebug)
        {
            Vector3 focalPlaneNormal = -Camera.main.transform.forward;
            Vector3 planeUp = Vector3.Cross(Vector3.Cross(focalPlaneNormal, Vector3.up), focalPlaneNormal);
            Gizmos.matrix = Matrix4x4.TRS(focusPointTarget, Quaternion.LookRotation(focalPlaneNormal, planeUp), new Vector3(4.0f, 3.0f, 0.01f));

            Color gizmoColor = Color.magenta;
            gizmoColor.a = 0.5f;
            Gizmos.color = gizmoColor;

            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }
}
