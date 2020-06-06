using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class CameraMotion : MonoBehaviour
{

    public float ScrollSpeed;
    float cameraDistanceMax = 100f;
    float cameraDistanceMin = 5f;
    float cameraDistance = 10f;
    float wheelScrollSpeed = 0.2f;
    public bool enableUpDownScroll = false;

    public float rotateSpeed;
    public float baseHeight = 30f;

    private float startXRot;


    private void Start()
    {
        startXRot = transform.rotation.eulerAngles.x;
    }

    void Update()
    {

        ScreenPanKeyboard();
        Scroll();

        float amount = gameObject.transform.position.y;
        ScrollSpeed = amount;

        if (Input.GetMouseButton(2))
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;
            Plane xzPlane = new Plane(Vector3.up, new Vector3(baseHeight, baseHeight, baseHeight));
            float distance = 0;

            if (xzPlane.Raycast(ray, out distance))
            {
                Vector3 pos = ray.GetPoint(distance);
                
                  
                gameObject.transform.RotateAround(pos, Vector3.up, Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime);
                if (enableUpDownScroll)
                {
                    gameObject.transform.RotateAround(pos, gameObject.transform.right, -Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime);
                }


            }

        }

    }


    void ScreenPanKeyboard()
    {
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            //transform.position += new Vector3(Time.deltaTime * ScrollSpeed, 0, 0);
            transform.position += transform.right * Time.deltaTime * ScrollSpeed;
        }

        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            //transform.position -= new Vector3(Time.deltaTime * ScrollSpeed, 0, 0);
            transform.position -= transform.right * Time.deltaTime * ScrollSpeed;
        }

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            //transform.position += new Vector3(0, 0, Time.deltaTime * ScrollSpeed);
            //transform.position += Vector3.forward * Time.deltaTime * ScrollSpeed;

            Vector3 forward = Camera.main.transform.TransformDirection(Vector3.forward);
            forward.y = 0;
            forward.Normalize();
            transform.position += forward * Time.deltaTime * ScrollSpeed;

        }

        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            //transform.position -= new Vector3(0, 0, Time.deltaTime * ScrollSpeed);
            //transform.position -= Vector3.forward * Time.deltaTime * ScrollSpeed;

            Vector3 back = Camera.main.transform.TransformDirection(Vector3.back);
            back.y = 0;
            back.Normalize();
            transform.position += back * Time.deltaTime * ScrollSpeed;
        }

    }


    void ScreenPanMouse()
    {
        if (Input.mousePosition.x >= Screen.width * 0.95)
        {
            transform.position += new Vector3(Time.deltaTime * ScrollSpeed, 0, 0);
        }

        if (Input.mousePosition.x <= Screen.width * 0.05)
        {
            transform.position -= new Vector3(Time.deltaTime * ScrollSpeed, 0, 0);
        }

        if (Input.mousePosition.y >= Screen.height * 0.95)
        {
            transform.position += new Vector3(0, 0, Time.deltaTime * ScrollSpeed);
        }

        if (Input.mousePosition.y <= Screen.height * 0.05)
        {
            transform.position -= new Vector3(0, 0, Time.deltaTime * ScrollSpeed);
        }
    }

    void Scroll()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        } else if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            float amount = gameObject.transform.position.y;

            Camera.main.transform.Translate(Vector3.forward * wheelScrollSpeed * amount);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            float amount = gameObject.transform.position.y;
            Camera.main.transform.Translate(Vector3.back * wheelScrollSpeed * amount);
        }
    }
}
