using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using Vuforia;

// extended functionality of the basic EventManager class.
// manages events pertaining to hardware, gestures, and gaze, and plinth activation. It handles all of the following:
//
//  - tap gestures
//  - Vuforia AR camera on / off
//  - keeping track of game objects that collide with the main camera's forward Raycaster ("gaze"), stored in the public var "GazedObject"
//  - sends messages to objects to let them know they have been "gazed" or "ungazed" if the object has an OSCEvents class component. 
//
public class CustomEventManager : MonoBehaviour
{
    public static CustomEventManager Instance { get; private set; }


    //are the keyboard controls active in the editor?
    public bool useDebugControls = true;

    //vuforia behavior instance
    public GameObject ARCamera;
    VuforiaBehaviour vuforia;

    // Represents the hologram that is currently being gazed at.
    public GameObject GazedObject;

    public GameObject CloseObject;

    public GameObject FocusPointManager;

    private List<GameObject> plinthList;

    private FocusPoint fp;


    GestureRecognizer recognizer;

    private bool tapFlag = false;

    private bool anchored;

    public bool ARTracking;

    public float activationRingRadius = 10f;
    public bool visualizeActivationRing = false;
    public bool visualizeFocusPoint = false;
    public GameObject focusPointVisualizer;
    private Vector3 focusPoint;
    private Plane[] frustrumPlanes;
    private Bounds[] bounds;

    private LineRenderer activationRingVisualizer;

    int layerMask;

    // Use this for initialization
    void Awake()
    {
        Instance = this;

        // Set up a GestureRecognizer to detect Select gestures.
        recognizer = new GestureRecognizer();
        recognizer.TappedEvent += (source, tapCount, ray) =>
        {
            tapFlag = true;
        };
        recognizer.StartCapturingGestures();

       

        //set up Vuforia AR tracking
        vuforia = ARCamera.GetComponent<VuforiaBehaviour>();

        layerMask = 1 << 2;
        layerMask = ~layerMask;

    }

    void Start()
    {


        frustrumPlanes = new Plane[6];
        bounds = new Bounds[plinthList.Count];
        //activationList = new DrawActive[plinthList.Count];

        int index = 0;
        /*foreach(GameObject go in plinthList)
        {
            activationList[index] = go.GetComponent<DrawActive>();
            index++;
        }*/

        //get remote control input for different states 
        RemoteControl.instance.AddParam("AR TRACKING: ", true, b => {
            ARCamera.SetActive(b);
        });

        RemoteControl.instance.AddParam("ACTIVATION RADIUS: ", activationRingRadius, x => {
            activationRingRadius = (float)x;
        }, 1, 20, 1);

        RemoteControl.instance.AddParam("VISUALIZE ACTIVATION RING: ", true, b => {
            visualizeActivationRing = b;
        });

        fp = new FocusPoint();

        RemoteControl.instance.AddParam("VISUALIZE STABILITY PLANE: ", true, b => {
            visualizeFocusPoint = b;
        });

        activationRingVisualizer = this.GetComponent<LineRenderer>();

        //#if UNITY_EDITOR
        ARTracking = true;
//#endif
    }

    // Update is called once per frame
    void Update()
    {

        setFocusPoint();

        if (visualizeActivationRing)
        {
            activationRingVisualizer.enabled = true;
            float theta = 0f;
            int size = (int)((1f / 0.01f) + 1f);
            activationRingVisualizer.numPositions = size;
            for (int i = 0; i < size; i++)
            {
                theta += (2.0f * Mathf.PI * 0.01f);
                float x = activationRingRadius * Mathf.Cos(theta) + Camera.main.transform.position.x;
                float z = activationRingRadius * Mathf.Sin(theta) + Camera.main.transform.position.z;
                activationRingVisualizer.SetPosition(i, new Vector3(x, Camera.main.transform.position.y-1.5f, z));
            }
        }
        else
        {
            activationRingVisualizer.enabled = false;
        }


        //for debugging tap events in editor via mouse click
        if (useDebugControls)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                tapFlag = true;
            }
        }
        // Figure out which hologram is focused this frame.
        GameObject oldGazedObject = GazedObject;
        GameObject oldCloseObject = CloseObject;

        // Do a raycast into the world based on the user's
        // head position and orientation.
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        RaycastHit hitInfo;
        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo, Mathf.Infinity, layerMask))
        {
            // If the raycast hit a hologram, use that as the focused object.
            GazedObject = hitInfo.collider.gameObject;

            // GazedObject.SendMessage("OnSelect");
        }
        else
        {
            // If the raycast did not hit a hologram, clear the focused object.
            GazedObject = null;
        }

        //if an object gazed upon and it's a new gaze, tell that object (used to pass OSC message to plinth)
        if (GazedObject != oldGazedObject && GazedObject != null)
        {
            recognizer.CancelGestures();
            recognizer.StartCapturingGestures();

            GazedObject.SendMessage("OnGaze");


            if (oldGazedObject != null)
            {
                oldGazedObject.SendMessage("OnUngaze");
            }
        }

        //not gazing at something but last frame something was gazed at (used to tell a lit plinth to turn its lights off)
        else if (GazedObject == null && oldGazedObject != null)
        {
            oldGazedObject.SendMessage("OnUngaze");
        }


        //handle tapping on gazed object
        
        if (GazedObject != null && tapFlag)
        {
            GazedObject.SendMessage("OnTapped");
        }
        
    
        tapFlag = false;      
    }
    /* Three-step plinth activation and focus-point-assignment process:
     *  1. get all plinth objects within camera's "Activation ring" (actually a sphere...)
     *  2. from that group, get plinths inside camera's frustum
     *  3. among those plinths, activate / set focus point to plinth that is closest to the Hololens's center of gaze
     */
    void setFocusPoint()
    {
        int index;
        GameObject target = null;

        if (visualizeActivationRing)
        {
            //draw ring around camera's Vector3.up axis
        }

        float smallestDist = 100000; //arbitrarily large distance to init smallest distance value
        //get all plinth objects in camera's activation ring

        if (bounds[0].center != plinthList[0].GetComponent<Collider>().bounds.center)
        {
            index = 0;
            foreach (GameObject plinth in plinthList)
            {
                bounds[index] = plinth.GetComponent<Collider>().bounds;
                index++;
            }
        }

        //update camera frustum. Used to calculate if the camera is looking at a plinth (must be done every frame because camera always moves)
        frustrumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        index = 0;
        foreach (GameObject go in plinthList)
        {
            //inside sphere
            if (Vector3.Distance(Camera.main.transform.position, go.transform.position) < activationRingRadius)
            {
                //inside frustum
                if (    GeometryUtility.TestPlanesAABB(frustrumPlanes, bounds[index]) &&
                        !go.GetComponent<Collider>().bounds.Contains(Camera.main.transform.position))
                {
                    //find plinth closest to center of view
                    Transform oldParent = go.transform.parent;
                    go.transform.parent = Camera.main.transform;
                    float dist = Mathf.Abs(go.transform.localPosition.x);
                    go.transform.parent = oldParent;

                  /*  bool ignoreCenter;
                    if (globals.mode == Globals.Mode.MODE_ACTIVE)
                    {
                        ignoreCenter = go != plinthList[plinthList.Count - 1] ? false : true;
                    }
                    else
                    {
                        ignoreCenter = false;
                    }*/
                    if(dist < smallestDist) // <- work around that ignores the central reference point if in active mode
                    {
                        smallestDist = dist;
                        target = go;
                    }
                }           
            }
            index++;
        }
        
        //use DrawActive class (if available) enable/disable children objects 
       

        //move the focusPointVisualizer object (if set in editor) to the target's location

        
        if (focusPointVisualizer!=null)
        {
            focusPointVisualizer.SetActive(visualizeFocusPoint);
            if (visualizeFocusPoint)
            {
                if (target != null)
                    focusPointVisualizer.transform.position = target.transform.position;
                else
                    focusPointVisualizer.transform.position = Camera.main.transform.position;
            }

        }


        //set focus point for stability plane to the position of the target object

        if (target == null)
        {
            //there is no plinth visible or within the activation circle. Put focus plane at default position (center of planes, where the red cross is?)
           // UnityEngine.VR.WSA.HolographicSettings.SetFocusPointForFrame(GameObject.Find("PlinthManager").transform.position, -Camera.main.transform.forward);
        }else
        {
            UnityEngine.VR.WSA.HolographicSettings.SetFocusPointForFrame(target.transform.position, -Camera.main.transform.forward);
        }
        // activate the selected plinth, deactivate the others. 
        
    }

}