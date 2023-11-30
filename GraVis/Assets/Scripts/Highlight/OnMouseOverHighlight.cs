using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnMouseOverHighlight : MonoBehaviour
{
    static public int MODE = 0;
    private int _mode = 0;
    // Determine which axis this is 
    enum Axis { X, Y, Z, Arbitrary }
    [SerializeField] Axis axis;
    enum TransformType { Rotation, Translation }
    [SerializeField] TransformType transformType;

    public ContextManager Context;

    public Vector3 TranslationAxis;

    private Color startcolor;

    private GameObject arrowHead;
    private GameObject arrowHead2;
    private GameObject arrowBody;

    private GameObject grandParent;
    private GameObject parent;

    private Collider m_collider;
    private Camera m_camera;

    private Vector3 oldMousePosition;
    private Vector3 mousedelta;

    private float dragSpeed = 0.001f;
    private float rotationDragSpeed = 0.1f;
    private Vector3 transformAxis;

    bool move = false;
    bool translate = true;
    bool rotate = false;


    void Start()
    {
        m_camera = Camera.main;
        // Get arrow object 
        arrowBody = transform.GetChild(0).GetChild(0).gameObject;
        arrowHead = transform.GetChild(1).GetChild(0).gameObject;
        // The roration arrow has 2 heads
        if (transformType == TransformType.Rotation)
            arrowHead2 = transform.GetChild(2).GetChild(0).gameObject;

        // Parent = Axis, grandparent = Object that should be moved
        grandParent = transform.parent.gameObject.transform.parent.gameObject;
        parent = transform.parent.gameObject;

        //
        m_collider = GetComponent<Collider>();
        // Init for mosuedelta
        oldMousePosition = Input.mousePosition;

        // Start by showing nothing 
        if (transformType == TransformType.Translation)
        {
            arrowBody.SetActive(false);
            arrowHead.SetActive(false);
            m_collider.enabled = false;
        }
        else
        {
            arrowBody.SetActive(false);
            arrowHead.SetActive(false);
            arrowHead2.SetActive(false);
            m_collider.enabled = false;
        }
    }

    void Update()
    {
        AdjustPlacement();
        Context.ControlHandler.SetPerformanceNeed(1);
        GetMouseDelta();
        // Stop mosue is released stop movement
        if (Input.GetMouseButtonUp(0))
        {
            move = false;
        }

        SwitchMode();
        ExecuteMove();
        
    }

    private void AdjustPlacement()
    {
        if (Vector3.Dot(transform.right, m_camera.transform.forward) < 0.0f)
        {
            transform.localRotation *= Quaternion.Euler(0, 180, 0);
            transform.localPosition = new Vector3(transform.localPosition.x * -1, transform.localPosition.y * -1, transform.localPosition.z * -1);// transform.position.x * -1, transform.position.y * -1, transform.position.z * -1);
        }
    }

    public void SwitchToRotation()
    {
        print("Switched to Rotate");
        translate = false;
        rotate = true;


        if (transformType == TransformType.Translation)
        {
            arrowBody.SetActive(false);
            arrowHead.SetActive(false);
            m_collider.enabled = false;
        }
        else
        {
            arrowBody.SetActive(true);
            arrowHead.SetActive(true);
            arrowHead2.SetActive(true);
            m_collider.enabled = true;
        }
    }

    public void SwitchToTranslation()
    {
        print("Switched to Translate");
        translate = true;
        rotate = false;

        if (transformType == TransformType.Rotation)
        {
            arrowBody.SetActive(false);
            arrowHead.SetActive(false);
            arrowHead2.SetActive(false);
            m_collider.enabled = false;

        }
        else
        {
            arrowBody.SetActive(true);
            arrowHead.SetActive(true);
            m_collider.enabled = true;
            //transform.parent.gameObject.transform.rotation = Quaternion.identity;
        }
    }

    public void SwitchToNone()
    {
        if (transformType == TransformType.Translation)
        {
            arrowBody.SetActive(false);
            arrowHead.SetActive(false);
            m_collider.enabled = false;
        }
        else
        {
            arrowBody.SetActive(false);
            arrowHead.SetActive(false);
            arrowHead2.SetActive(false);
            m_collider.enabled = false;
        }
    }

    private void SwitchMode()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1) || _mode != MODE && MODE == 1)
        {
            _mode = MODE;
            SwitchToTranslation();
        }
        if (Input.GetKeyDown(KeyCode.Keypad2) || _mode != MODE && MODE == 2)
        {
            _mode = MODE;
            SwitchToRotation();
        }
        if (Input.GetKeyDown(KeyCode.Keypad0) || _mode != MODE && MODE == 0)
        {
            _mode = MODE;
            SwitchToNone();
        }
    }

    private void GetMouseDelta()
    {
        mousedelta = Input.mousePosition - oldMousePosition;
        oldMousePosition = Input.mousePosition;
    }

    private void ExecuteMove()
    {
        if (move)
        {
            if (translate)
            {
                switch (axis)
                {
                    case Axis.X:
                        grandParent.transform.localPosition += new Vector3((mousedelta.x * transformAxis.x + mousedelta.y * transformAxis.y) * dragSpeed, 0, 0);
                        break;
                    case Axis.Y:
                        grandParent.transform.localPosition += new Vector3(0, (mousedelta.x * transformAxis.x + mousedelta.y * transformAxis.y) * dragSpeed, 0);
                        break;
                    case Axis.Z:
                        grandParent.transform.localPosition += new Vector3(0, 0, (mousedelta.x * transformAxis.x + mousedelta.y * transformAxis.y) * dragSpeed);
                        break;
                    case Axis.Arbitrary:
                        grandParent.transform.localPosition += gameObject.transform.up * (mousedelta.x * transformAxis.x + mousedelta.y * transformAxis.y) * dragSpeed * Context.CameraController.distance;
                        break;
                }
            }

            if (rotate)
            {
                switch (axis)
                {
                    case Axis.X:
                        grandParent.transform.Rotate(new Vector3((mousedelta.x * transformAxis.x + mousedelta.y * transformAxis.y) * rotationDragSpeed, 0, 0));
                        //transform.parent.gameObject.transform.rotation = Quaternion.identity;
                        break;
                    case Axis.Y:
                        grandParent.transform.Rotate(new Vector3(0, (mousedelta.x * transformAxis.x + mousedelta.y * transformAxis.y) * rotationDragSpeed, 0));
                        // transform.parent.gameObject.transform.rotation = Quaternion.identity;
                        break;
                    case Axis.Z:
                        grandParent.transform.Rotate(new Vector3(0, 0, (mousedelta.x * transformAxis.x + mousedelta.y * transformAxis.y) * rotationDragSpeed));
                        //transform.parent.gameObject.transform.rotation = Quaternion.identity;
                        break;
                }

            }
        }

    }

    void OnMouseEnter()
    {
        startcolor = arrowBody.GetComponent<Renderer>().material.color;
        arrowBody.GetComponent<Renderer>().material.color = Color.yellow;
        arrowHead.GetComponent<Renderer>().material.color = Color.yellow;
        if (transformType == TransformType.Rotation)
            arrowHead2.GetComponent<Renderer>().material.color = Color.yellow;
        
    }

    void OnMouseOver()
    {
        Context.ControlHandler.SetFocus(this.GetInstanceID());
        // If over Axsis and click left button enable movement
        if (Input.GetMouseButtonDown(0))
        {
            move = true;
            CalcTransfromAxis();

        }
    }
    void OnMouseExit()
    {
        arrowBody.GetComponent<Renderer>().material.color = startcolor;
        arrowHead.GetComponent<Renderer>().material.color = startcolor;
        if (transformType == TransformType.Rotation)
            arrowHead2.GetComponent<Renderer>().material.color = startcolor;
    }


    private void CalcTransfromAxis()
    {
        // Get Axsis direction on screen space
        Vector3 screenPos = m_camera.WorldToScreenPoint(transform.position);
        Vector3 screenPosUP = m_camera.WorldToScreenPoint(transform.position + transform.up/1000.0f);
        Debug.Log(transformAxis.z);
        transformAxis = screenPosUP - screenPos;
        transformAxis.z = 0;

        //transformAxis.Normalize();
        
        // If one direction is 0 the other must be 1 or -1
        if (transformAxis.x == 0)
        {
            transformAxis.x = 0;
            if (transformAxis.y >= 0)
                transformAxis.y = 1;
            if (transformAxis.y < 0)
                transformAxis.y = -1;
        }
        else if (transformAxis.y == 0)
        {

            transformAxis.y = 0;

            if (transformAxis.x >= 0)
                transformAxis.x = 1;
            if (transformAxis.x < 0)
                transformAxis.x = -1;
        }
        else
        {

            // Normalize slope 
            float max = 1;

            if (Mathf.Abs(transformAxis.x) > Mathf.Abs(transformAxis.y))
                max = Mathf.Abs(transformAxis.x);
            else
                max = Mathf.Abs(transformAxis.y);

            transformAxis.x = transformAxis.x / max;
            transformAxis.y = transformAxis.y / max;

        }
        
    }
}
