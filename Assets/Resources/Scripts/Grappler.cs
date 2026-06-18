using System.Xml.Serialization;
using UnityEngine;

public class Grappler : MonoBehaviour
{
    Vector3 aimPos;
    Camera cam;
    private void Start()
    {
        cam = Camera.main;
        
        aimPos = cam.ScreenToWorldPoint(new Vector2(0,0));
    }
    void Update()
    {
        Debug.DrawRay(cam.transform.position, cam.transform.forward * 20f, Color.red);    
        if (Input.GetMouseButtonDown(2))
        {   
            RaycastHit hit;

            if(Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 20f))
            {

            }
        }
    }
}
