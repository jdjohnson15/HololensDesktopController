using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Persistence;
using UnityEngine.UI;


//use store.Delete() to remove an anchor we previously saved and store.Clear() to remove all previously saved data.

public class WorldAnchorManager : MonoBehaviour {

    private WorldAnchorStore store;
    private bool savedRoot = false;
    private bool loadedStore = false;

    public GameObject debugTextGO;

    private DebugText debugText;

    private Queue<GameObject> storesToSaveQueue;

    public GameObject[] gameObjectsThatNeedAnchors;

    Globals globals;

    public bool clusters;

    class WorldAnchorTable
    {
        public string name;
        public UnityEngine.VR.WSA.WorldAnchor anchor;
    }

    public int numberOfAnchors = 4;

    private float orbitAngleInterval;

    private bool correctErrors = false;

    private List<WorldAnchorTable> waTable;

    // Use this for initialization
    void Start() {
        WorldAnchorStore.GetAsync(StoreLoaded);
        globals = GameObject.Find("Globals").GetComponent<Globals>();

        
        int errorIndex = 0;

        if (clusters)
       // foreach (GameObject go in gameObjectsThatNeedAnchors)
        //{
         RemoteControl.instance.AddParam("Correct errors: ", false, b =>
            {
                Debug.Log("error index: "+errorIndex);
                correctErrors = b;
            });
          //  errorIndex++;
        //}

        storesToSaveQueue = new Queue<GameObject>();
        waTable = new List<WorldAnchorTable>();

        if (debugTextGO != null)
            debugText = debugTextGO.GetComponent<DebugText>();
        /*
        RemoteControl.instance.AddParam("WORLD ANCHOR CLUSTERS: ", clusters, c =>{
            clusters = c;
            bool clustersPresent = (gameObjectsThatNeedAnchors[0].transform.FindChild("anchor clustor"));

            //create anchor cluster objects for all game objects that need them
            if (clusters && !clustersPresent)
            {
                foreach (GameObject go in gameObjectsThatNeedAnchors)
                {
                    GameObject anchorCluster = new GameObject("anchor cluster");
                    anchorCluster.transform.parent = go.transform;
                    anchorCluster.transform.localPosition = Vector3.zero;

                    for (int i = 0; i < 4; i++) //make into parameter passed in by remote if this works -> rapidly test optimal anchor count
                    {
                        GameObject anchor = GameObject.CreatePrimitive(PrimitiveType.Cube); //new GameObject("a" + i);
                        anchor.name = "a" + i;
                        anchor.transform.localScale = Vector3.one * 0.01f;
                        anchor.transform.parent = anchorCluster.transform;
                        anchor.transform.localPosition = Quaternion.Euler(0, i * 90, 0) * Vector3.right; //should be one meter away from root object
                    }

                }
            }
            else if (!clusters && clustersPresent) //destroys anchor cluster objects if the option is disabled
            {
                foreach (GameObject go in gameObjectsThatNeedAnchors)
                {
                    GameObject ac = go.transform.FindChild("anchor cluster").gameObject;
                    if (ac != null)
                    {
                        UnityEngine.VR.WSA.WorldAnchor wa;
                        for (int i = 0; i<ac.transform.childCount; i++)
                        {
                            wa = ac.transform.GetChild(i).GetComponent<UnityEngine.VR.WSA.WorldAnchor>();
                            if (wa != null)
                                destroyWorldAnchor(ac.transform.GetChild(i).gameObject);
                        }
                        
                        Destroy(ac);
                    }
                }
            }
        });
        */
#if UNITY_EDITOR
        loadedStore = true;
#endif
    }
    // Update is called once per frame
    void Update() {

        if (loadedStore)
        {
            foreach (GameObject go in gameObjectsThatNeedAnchors)
            {
                bool anchored = true;
                if (!clusters)
                {                   
                    WorldAnchorTable temp = new WorldAnchorTable();
#if NETFX_CORE
                    if (this.store.Load(go.name, go))
                    {


                        Debug.Log("Loading World Anchor for " + go.name + " -- SUCCESS");
                        //go.GetComponent<PlinthManager>().setState(2);
                        anchored = true;
                    }
                    else
                    {
                        Debug.Log("Loading World Anchor for " + go.name + " -- FAIL");
                        anchored = false;
                        //go.GetComponent<PlinthManager>().setState(0);
                        //go.AddComponent<UnityEngine.VR.WSA.WorldAnchor>();
                        //Debug.Log("New World Anchor component added to " + go.name);
                    }
#endif
                    temp.name = go.name;
                    temp.anchor = go.GetComponent<UnityEngine.VR.WSA.WorldAnchor>();
                    waTable.Add(temp);                   
                }

                //experimental code for anchor point clusters
                else
                {
                   // GameObject anchorCluster = new GameObject("anchor cluster");
                    //anchorCluster.transform.parent = go.transform;
                    //anchorCluster.transform.localPosition = Vector3.zero;

                    orbitAngleInterval = 360f / (numberOfAnchors);

                    for (int i = 0; i < numberOfAnchors; i++) 
                    {
                        GameObject anchor = GameObject.CreatePrimitive(PrimitiveType.Cube); //new GameObject("a" + i);
                        anchor.name = go.name+"_a" + i;
                        anchor.transform.localScale = Vector3.one * 0.01f;
                        //anchor.transform.parent = anchorCluster.transform;
                        anchor.transform.position = go.transform.position + go.transform.rotation * Quaternion.Euler(0, i * orbitAngleInterval, 0) * Vector3.right; //should be one meter away from root object
                        anchor.transform.rotation = go.transform.rotation * Quaternion.Euler(0, i * orbitAngleInterval, 0);

                        WorldAnchorTable temp = new WorldAnchorTable();
#if NETFX_CORE
                        if (this.store.Load(go.name+"_a"+i, anchor))
                        {
                            Debug.Log("Loading World Anchor for " + go.name + "_a"+ i + " -- SUCCESS");
                            //go.GetComponent<PlinthManager>().setState(2);
                            //anchored = true;
                        }
                        else
                        {
                            Debug.Log("Loading World Anchor for " + go.name + "_a"+ i + " -- FAIL");
                            anchored = false;//if one anchor in the cluster fails to load, the whole object fails.

                            //go.GetComponent<PlinthManager>().setState(0);
                            //go.AddComponent<UnityEngine.VR.WSA.WorldAnchor>();
                            //Debug.Log("New World Anchor component added to " + go.name);
                        }
#endif
                        temp.name = go.name + "_a"+i;
                        temp.anchor = anchor.GetComponent<UnityEngine.VR.WSA.WorldAnchor>();
                        waTable.Add(temp);
                        
                    }

                }

                RemoteControl.instance.AddParam("WORLD ANCHOR PLINTH " + go.name + ": ", anchored, anchoredInput => {
                    if (anchoredInput)
                    {
                        //create world anchor for gameobject (tell plinthmanager of state change)
                        if (!clusters)
                        {
                            if (!go.GetComponent<UnityEngine.VR.WSA.WorldAnchor>())
                            {
                                go.AddComponent<UnityEngine.VR.WSA.WorldAnchor>();
                                saveWorldAnchor(go);
                            }                               
                            
                            
                        }
                        else
                        {
                            for (int i = 0; i < numberOfAnchors; i++)
                            {
                                GameObject a = GameObject.Find(go.name + "_a" + i);
                                if (!a.GetComponent<UnityEngine.VR.WSA.WorldAnchor>())
                                    a.AddComponent<UnityEngine.VR.WSA.WorldAnchor>();                               
                            }
                            saveWorldAnchor(go);
                        }

                        //go.GetComponent<PlinthManager>().setState(2);
                    }
                    else
                    {
                        //destroy existing world anchor without assigning a new one (tell plinthmanager of state change)
                        destroyWorldAnchor(go);
                        //go.GetComponent<PlinthManager>().setState(0);

                    }
                });

            }
            loadedStore = false;
        }
        if (!savedRoot && globals.mode == Globals.Mode.MODE_ACTIVE)
        {
            onSaveAll();
        }
        if (globals.mode == Globals.Mode.MODE_SETUP)
        {
            savedRoot = false;
        }


        //update object positions based on the locations of their cluster objects
        //need decay timer
        //only update active object (processing all anchors all the time is expensive)
        if (clusters)
        {
            int errorIndex = 0;
            foreach(GameObject go in gameObjectsThatNeedAnchors)
            {
                //the cluster has actual anchors 

                //maybe a better heuristic should be here? 

                if (GameObject.Find(go.name + "_a0").GetComponent<UnityEngine.VR.WSA.WorldAnchor>())
                {
                    float x = 0f, y = 0f, z = 0f;


                    Vector3[] anchorPositions = new Vector3[numberOfAnchors];
                    for (int i = 0; i < numberOfAnchors; i++)
                    {
                        GameObject a = GameObject.Find(go.name + "_a" + i);
                        anchorPositions[i] = a.transform.position;
                        
                        float errorMagnitude = Vector3.Distance(a.transform.position, go.transform.position) - 1f;

                        Vector3 errorAdjustment;
                        if (correctErrors)
                        {
                            errorAdjustment = errorMagnitude * Vector3.Normalize(go.transform.position - a.transform.position);
                            //Debug.Log("error adjustment: " + errorAdjustment);
                        }
                        else
                        {
                            errorAdjustment = Vector3.zero;
                            //Debug.Log("error adjustment: none");
                        }

                        x += a.transform.position.x + errorAdjustment.x;
                        y += a.transform.position.y + errorAdjustment.y;
                        z += a.transform.position.z + errorAdjustment.z;
                    }

                    x /= numberOfAnchors;
                    y /= numberOfAnchors;
                    z /= numberOfAnchors;

                    go.transform.position = new Vector3(x, y, z);
                    go.transform.rotation = GameObject.Find(go.name + "_a0").transform.rotation; //anchor a0 is always aligned with the root object's intended rotation

                    //calculate error
                    if (correctErrors)
                        printDebug(go.name + "\nerror: "+detectError(anchorPositions, go.transform.position).ToString("0.00000"), go);
                    else
                        printDebug(go.name + "\nerror (no correction): " + detectError(anchorPositions, go.transform.position).ToString("0.00000"), go);
               
                }
                //the cluster is still unanchored and moves with the object
                else
                {
                    for (int i = 0; i < numberOfAnchors; i++)
                    {
                        GameObject a = GameObject.Find(go.name + "_a" + i);
                        a.transform.position = go.transform.position + go.transform.rotation * Quaternion.Euler(0, i * orbitAngleInterval, 0) * Vector3.right;
                        a.transform.rotation = go.transform.rotation * Quaternion.Euler(0, i * orbitAngleInterval, 0);
                    }
                    printDebug(go.name+ "\nnot anchored", go);
                }
                errorIndex++;
            }
            
        }
    }

    private float detectError(Vector3[] a, Vector3 root)
    {
        float error = 0f;
        foreach(Vector3 v in a)
        {
            error += Vector3.Distance(v, root) - 1f;
        }
        error /= a.Length;
        return error;
    }


    void OnApplicationQuit()
    {
        onSaveAll();
    }
    private void StoreLoaded(WorldAnchorStore store)
    {
        this.store = store;
        loadedStore = true;
    }

    void seeAllAnchorIDs()
    {
#if NETFX_CORE

        string[] ids = this.store.GetAllIds();
        for (int index = 0; index < ids.Length; index++)
        {
            Debug.Log(ids[index]);
        }

#endif
    }

    void onSaveAll()
    {
        foreach (GameObject go in gameObjectsThatNeedAnchors)
        {
            saveWorldAnchor(go);
            savedRoot = true;
        }
    }

    public void saveWorldAnchor(GameObject go)
    {
        UnityEngine.VR.WSA.WorldAnchor attachingAnchor;
        if (!clusters)
        {
            attachingAnchor = go.GetComponent<UnityEngine.VR.WSA.WorldAnchor>();
            if (attachingAnchor == null)
            {
                Debug.Log("World anchor for " + go.name + " either missing or not created");
            }
            else
            {
                int index = 0;
                Debug.Log("Attempting to save world anchor for " + go.name);
                foreach (WorldAnchorTable wat in waTable)
                {
                    if (wat.name == go.name)
                    {
                        break;
                    }
                    index++;
                }

                waTable[index].anchor = attachingAnchor;

                attachingAnchor.OnTrackingChanged += AttachingAnchor_OnTrackingChanged;
                if (attachingAnchor.isLocated)
                {
                    destroyWorldAnchor(go);
                    bool saved = this.store.Save(go.name, attachingAnchor);
                    Debug.Log("Saving persisted position of " + go.name + " immediately to store: " + saved);
                }
                else
                {
                    //storesToSaveQueue.Enqueue(go);
                    Debug.Log(go.name + " queued for world anchor store");
                    //attachingAnchor.OnTrackingChanged += AttachingAnchor_OnTrackingChanged;
                }
                Debug.Log("anchors after save of " + go.name + ": ");
                seeAllAnchorIDs();
            }

        }
        else
        {
            //GameObject ac = go.transform.FindChild("anchor cluster").gameObject;
            //if (ac != null)
            // {
            bool allChildrenLocated = true; //if one isn't found immediately, trip to false
            for (int i = 0; i < numberOfAnchors; i++)
            {
                attachingAnchor = GameObject.Find(go.name + "_a" + i).GetComponent<UnityEngine.VR.WSA.WorldAnchor>();
                if (attachingAnchor == null)
                {
                    Debug.Log("World anchor for " + go.name + " either missing or not created");
                }
                else
                {
                    int index = 0;
                    Debug.Log("Attempting to save world anchor for " + go.name);
                    foreach (WorldAnchorTable wat in waTable)
                    {
                        if (wat.name == go.name + "_a" + i)
                        {
                            break;
                        }
                        index++;
                    }

                    waTable[index].anchor = attachingAnchor;

                    attachingAnchor.OnTrackingChanged += AttachingAnchor_OnTrackingChanged;
                    if (attachingAnchor.isLocated)
                    {

                    }
                    else
                    {
                        //storesToSaveQueue.Enqueue(go);
                        Debug.Log(go.name + "_a" + i + " queued for world anchor store");
                        //attachingAnchor.OnTrackingChanged += AttachingAnchor_OnTrackingChanged;
                        allChildrenLocated = false;
                    }
                }
            }
            if (allChildrenLocated)
            {
                destroyWorldAnchor(go);
                for (int i = 0; i < numberOfAnchors; i++)
                {
#if NETFX_CORE
                    bool saved = this.store.Save(go.name + "_a" + i, GameObject.Find(go.name + "_a" + i).gameObject.GetComponent<UnityEngine.VR.WSA.WorldAnchor>());

                    Debug.Log("Saving persisted positions of " + go.name + " immediately to store: " + saved);
#endif
                }
            }
        }

         //   }
      //  }               
    }

    public void destroyWorldAnchor(GameObject go)
    {
        string[] ids;
        // if (loadedStore)
        // {
#if NETFX_CORE
        ids = this.store.GetAllIds();
        if (!clusters)
        {
            UnityEngine.VR.WSA.WorldAnchor anchor = go.GetComponent<UnityEngine.VR.WSA.WorldAnchor>();
            if (anchor != null)
            {
                Debug.Log("World anchor for " + go.name + " destroyed");
                DestroyImmediate(anchor);
            }
            else
            {
                Debug.Log("World anchor for " + go.name + " not destroyed because it doesn't exist");
            }
            foreach (string id in ids)
            {
                if (id == go.name)
                {
                    bool deleted = this.store.Delete(id);
                    Debug.Log("deleted: " + deleted);
                    break;
                }
            }
            // }
            savedRoot = false;
            //Debug.Log("anchors after deletion of " + go.name + ": ");
            seeAllAnchorIDs();
        }
        
        else{
           // GameObject ac = go.transform.FindChild("anchor cluster").gameObject;

            UnityEngine.VR.WSA.WorldAnchor anchor;
            for (int i = 0; i < numberOfAnchors; i++)
            {
                anchor = GameObject.Find(go.name+"_a"+i).GetComponent<UnityEngine.VR.WSA.WorldAnchor>();

                if (anchor != null)
                    DestroyImmediate(anchor);

                foreach (string id in ids)
                {
                    if (id == go.name + "_a" + i)
                    {
                        bool deleted = this.store.Delete(id);
                        Debug.Log("deleted: " + deleted);
                        break;
                    }
                }
                // }
                savedRoot = false;
                //Debug.Log("anchors after deletion of " + go.name + ": ");
                seeAllAnchorIDs();
                
            }
        }
#endif
    }

    private void AttachingAnchor_OnTrackingChanged(UnityEngine.VR.WSA.WorldAnchor self, bool located)
    {
        Debug.Log("tracking changed!");
  
        /* GameObject go = storesToSaveQueue.Dequeue();

            destroyWorldAnchor(go);
            bool saved = this.store.Save(go.name, self);
            // go.AddComponent<UnityEngine.VR.WSA.WorldAnchor>();  self;
            Debug.Log("Saving persisted position of " + go.name + " in callback: "+saved);
            //self.OnTrackingChanged -= AttachingAnchor_OnTrackingChanged;*/
        foreach (WorldAnchorTable wat in waTable)
        {
            if (wat.anchor == self)
            {
                if (located)
                {
                    Debug.Log("tracking found!");
                    //destroyWorldAnchor(GameObject.Find(wat.name));
                    // if (loadedStore)
                    // {
                    foreach (string id in this.store.GetAllIds())
                    {
                        if (id == wat.name)
                        {
                            bool deleted = this.store.Delete(id);
                            Debug.Log("deleted: " + deleted);
                            break;
                        }
                    }
                    //}
                    bool saved = this.store.Save(wat.name, self);
                    Debug.Log("Saving persisted position of " + wat.name + " in callback: " + saved);
                    //printDebug("tracking of " + wat.name + "found and saved!");
                    break;
                }
                else
                {
                    Debug.Log("tracking lost!");
                    //printDebug("tracking of " + wat.name + "lost!");
                }
            }
        }
        
    }

    private void printDebug(string message, GameObject go)
    {
        go.transform.FindChild("UITextPrefab").FindChild("Text").GetComponent<DebugText>().SetMessage(message);
    } 
}
