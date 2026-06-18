using System.Xml.Serialization;
using UnityEngine;

public class PlayerControler : MonoBehaviour
{
    public float defaultWalkSpeed = 5f;
    public float runSpeedMultiplier = 1.25f;

    public float mouseSensitivity = 10f;
    public float xRotation = 0f;
    public float yRotation = 0f;


    [SerializeField]
    private float finalSpeed = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;   // 마우스 커서를 화면 안에서 고정
        Cursor.visible = false;
    }
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        //이동 방향
        Vector3 moveDir = new Vector3(horizontal, 0, vertical);

        //화면 관련
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        playerRotation();

        //이동 관련
        if (Input.GetKey(KeyCode.LeftShift))
        {
            finalSpeed = defaultWalkSpeed * runSpeedMultiplier;
        }
        else
        {
            finalSpeed = defaultWalkSpeed;
        }

        //최종 이동
        transform.Translate(moveDir * finalSpeed * Time.deltaTime);
    }

    void playerRotation()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        Camera.main.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);

        transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
