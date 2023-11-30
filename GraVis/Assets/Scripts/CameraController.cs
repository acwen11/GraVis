using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraVisUI;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public enum CameraFreedom
{
    Free,
    FixX,
    FixY,
    FixZ
}

public class CameraController : MonoBehaviour
{
    public GameObject ObjectOfInterest;
    public Vector3 Center = new Vector3(0, 0, 0);
    [HideInInspector]
    public float distance = 1.0f;
    public CameraFreedom Freedom;
    public ControlHandler ControlHandler;
    public string ControlHandlerName = "CameraControl";

    public float Quality = 5.0f;
    private Camera _camera;
    private float zoomFactor;

    public GameObject CenterObject;

    public bool CameraIsDirty; // This bool is true, if the camera has actually been moved

    public UnityEvent EventsOnClick;

    static Vector3 BASICCENTER = new Vector3(0, 0, 0);
    static float BASICDISTANCE = 1.0f;
    static float BASICZOOMFACTOR = 0.1f;
    static Vector3 BASICPOSITION = new Vector3(0, -0.3f, 0.2f);

    void Start()
    {
        Freedom = CameraFreedom.Free;
        CameraIsDirty = false;
        zoomFactor = 0.1f;
        distance = Mathf.Exp(zoomFactor);
        
        _camera = GetComponent<Camera>();
        _camera.transform.position = (_camera.transform.position - Center).normalized * distance;
        _camera.depthTextureMode = DepthTextureMode.Depth;

        _camera.transform.LookAt(Center);
    }

    void OnPreCull()
    {
        _camera.cullingMatrix = Matrix4x4.Ortho(-99999, 99999, -99999, 99999, 0.001f, 99999) *
                            Matrix4x4.Translate(Vector3.forward * -99999 / 2f) *
                            _camera.worldToCameraMatrix;
    }

    public Vector3 ReadVector(string Line)
    {
        if (Line.StartsWith("(") && Line.EndsWith(")"))
        {
            Line = Line.Substring(1, Line.Length - 2);
        }

        // split the items
        string[] sArray = Line.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(sArray[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(sArray[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
        return result;
    }

    public Quaternion ReadQuat(string Line)
    {
        if (Line.StartsWith("(") && Line.EndsWith(")"))
        {
            Line = Line.Substring(1, Line.Length - 2);
        }

        // split the items
        string[] sArray = Line.Split(',');

        // store as a Vector3
        Quaternion result = new Quaternion(
            float.Parse(sArray[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(sArray[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(sArray[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(sArray[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
        return result;
    }

    public void LoadCameraFromFile()
    {
        StreamReader reader = new StreamReader("Assets/Scripts/Camera.txt");

        Center = ReadVector(reader.ReadLine());
        
        distance = float.Parse(reader.ReadLine().Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        zoomFactor = float.Parse(reader.ReadLine().Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        _camera.transform.position = ReadVector(reader.ReadLine());
        Vector3 rot = ReadVector(reader.ReadLine());
        _camera.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);
        reader.Close();
    }

    public void SaveCameraSettings()
    {
        StreamWriter CameraFile = new StreamWriter("Assets/Scripts/Camera.txt");

        CameraFile.WriteLine(Center.ToString());
        CameraFile.WriteLine(distance.ToString());
        CameraFile.WriteLine(zoomFactor.ToString());
        Vector3 Pos = _camera.transform.position;
        Vector3 Rotation = _camera.transform.rotation.eulerAngles;
        CameraFile.WriteLine(Pos.ToString());
        CameraFile.WriteLine(Rotation.ToString());
        CameraFile.Close();
    }

    public void ResetCamera()
    {
        Center = BASICCENTER;
        distance = BASICDISTANCE;
        zoomFactor = BASICZOOMFACTOR;
        _camera.transform.position = Center + (BASICPOSITION - Center).normalized * distance;
        _camera.transform.LookAt(Center);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.C))
        {
            SaveCameraSettings();
        }

        if (Input.GetKeyUp(KeyCode.L))
        {
            LoadCameraFromFile();
        }

        if (Input.GetKey(KeyCode.Space))
        {
            ResetCamera();
        }

        if (Input.mouseScrollDelta.y != 0 && !ControlHandler.IsOverUI() && ControlHandler.IsApplicationFocused() && !ControlHandler.IsMouseDragging(this.GetInstanceID(), 1))
        {
            zoomFactor -= 0.1f * Input.mouseScrollDelta.y;
            distance = Mathf.Exp(zoomFactor);
            _camera.transform.position = Center + (_camera.transform.position - Center).normalized * distance;
            ControlHandler.SetPerformanceNeed(2);
        }

        if (ControlHandler.IsMouseDragging(this.GetInstanceID(), 2) && Input.mouseScrollDelta.y == 0)
        {
            ControlHandler.SetPerformanceNeed(2);
            Vector3 camPrevious = _camera.transform.position;
            _camera.transform.Translate(_camera.transform.right * 0.04f * distance * -Input.GetAxis("Mouse X"), Space.World);
            _camera.transform.Translate(_camera.transform.up * 0.04f * distance * - Input.GetAxis("Mouse Y"), Space.World);
            //Center += (_camera.transform.position - camPrevious);
        }

        if (ControlHandler.IsMouseDragging(this.GetInstanceID(), 1) && Input.mouseScrollDelta.y != 0)
        {
            //zoomFactor -= 0.1f * Input.mouseScrollDelta.y;
            Center += (_camera.transform.forward * 0.02f * Input.mouseScrollDelta.y);
            distance = Mathf.Exp(zoomFactor);
            _camera.transform.position = Center + (_camera.transform.position - Center).normalized * distance;
            ControlHandler.SetPerformanceNeed(2);
        }

        if (ControlHandler.IsMouseClicked(this.GetInstanceID(), 0) && ControlHandler.IsMouseClicked(this.GetInstanceID(), 1))
        {
            Center = new Vector3(0.0f, 0.0f, 0.0f);
        }

        if (ControlHandler.IsMouseDragging(this.GetInstanceID(), 1))
        {

            ControlHandler.SetPerformanceNeed(1);
            if (Freedom == CameraFreedom.Free)
            {
                _camera.transform.RotateAround(Center, _camera.transform.forward, 2.0f * Input.GetAxis("Mouse X"));
                _camera.transform.RotateAround(Center, _camera.transform.forward, 2.0f * Input.GetAxis("Mouse Y"));
            }
        }

        if (ControlHandler.IsMouseDragging(this.GetInstanceID()))
        {
            ControlHandler.SetPerformanceNeed(1);
            switch (Freedom)
            {
                case CameraFreedom.FixX:
                    _camera.transform.RotateAround(Center, Vector3.right, distance * 10.0f * Input.GetAxis("Mouse X"));
                    _camera.transform.RotateAround(Center, Vector3.right, distance * 10.0f * Input.GetAxis("Mouse Y"));
                    break;
                case CameraFreedom.FixY:
                    _camera.transform.RotateAround(Center, Vector3.up, distance * 10.0f * Input.GetAxis("Mouse X"));
                    _camera.transform.RotateAround(Center, Vector3.up, distance * 10.0f * Input.GetAxis("Mouse Y"));
                    break;
                case CameraFreedom.FixZ:
                    _camera.transform.RotateAround(Center, Vector3.forward, distance * 10.0f * Input.GetAxis("Mouse X"));
                    _camera.transform.RotateAround(Center, Vector3.forward, distance * 10.0f * Input.GetAxis("Mouse Y"));
                    break;
                default: // Free is default
                    _camera.transform.RotateAround(Center, _camera.transform.up, 10.0f * Input.GetAxis("Mouse X"));
                    _camera.transform.RotateAround(Center, _camera.transform.right, 10.0f * -Input.GetAxis("Mouse Y"));
                    break;
            }

            if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
                CameraIsDirty = true;
        }

        if (CameraIsDirty)
            ControlHandler.SetPerformanceNeed(2);

        if (ControlHandler.IsMouseClicked(this.GetInstanceID()))
        {
            if (!CameraIsDirty)
            {
                EventsOnClick.Invoke();
            }
            else
            {
                // camera is dirty and has been released
                Debug.Log("Recompute shaders");
                CameraIsDirty = false;
                
            }
        }
        CenterObject.transform.position = Center;
    }
}
