using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenObject : MonoBehaviour {

    public GameObject imageTarget0;
    public GameObject imageTarget1;
    public GameObject imageTarget2;

    private bool tracking = true;

    // Use this for initialization
    void Start () {
        RemoteControl.instance.AddParam("AR Tracking: ", true, b =>
        {
            tracking = b;
        });
	}
	
	// Update is called once per frame
	void Update () {
		if (tracking)
        {
            float x = Vector3.Distance(imageTarget0.transform.position, imageTarget1.transform.position);
            float y = Vector3.Distance(imageTarget0.transform.position, imageTarget2.transform.position);
            gameObject.transform.localScale = new Vector3(x, y, 0.05f);

            Vector3 normal = Vector3.Cross(imageTarget1.transform.position - imageTarget0.transform.position, imageTarget2.transform.position - imageTarget0.transform.position);

            gameObject.transform.rotation = Quaternion.LookRotation(normal, imageTarget2.transform.position - imageTarget0.transform.position);
            Vector3 t = imageTarget1.transform.position + imageTarget2.transform.position;
            gameObject.transform.position = new Vector3(t.x / 2, t.y / 3, t.z / 2);
            UnityEngine.VR.WSA.HolographicSettings.SetFocusPointForFrame(this.transform.position, -Camera.main.transform.forward);
        }

    }
}
