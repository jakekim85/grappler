using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

public class Grappler : MonoBehaviour
{
    [SerializeField]
    private bool isColliderInShotRange = false;
    public bool isSwing=false;

    private Vector3 grabPos;

    Vector3 aimPos;
    Camera cam;
    private void Start()
    {
        cam = Camera.main;
    }
    void Update()
    {
        RaycastHit hit;

        //벽 감지
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 20f))
        {
            isColliderInShotRange=true;
            Debug.DrawRay(cam.transform.position, cam.transform.forward * hit.distance, Color.red);
        }
        else
        {
            isColliderInShotRange=false;
            Debug.DrawRay(cam.transform.position, cam.transform.forward * 20f, Color.red);
        }

        //발사시
        if (Input.GetMouseButtonDown(1))
        {
            if (isColliderInShotRange) { 

                if(hit.collider.tag == "Wall")
                {
                    grabPos = hit.transform.position;

                    isSwing = true;
                    Debug.Log("asdfasdfas");
                }
            }
        }

        

    }
}
