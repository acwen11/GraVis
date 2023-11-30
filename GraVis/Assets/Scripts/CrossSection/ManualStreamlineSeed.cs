using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ManualStreamlineSeed : MonoBehaviour
{
    public enum SymmetryAxis
    {
        zAxis,
        yAxis,
        xAxis,
        planeNormal
    }

    public int SymmetryPointsCount = 0;

    public bool UseSymmetry = true;
    public CrossSection crossSection;
    public GameObject SelectionPointPrefab;
    public ContextManager context;
    public Camera cam;

    public StreamlineGenerator StreamlineGenerator;

    private Vector3 Axis;
    [SerializeField]
    private SymmetryAxis symmetryAxis;

    private List<GameObject> selectionPoints;
    private List<Quaternion> symmetryAngles;
    private bool isActive = true;

    void Start()
    {
        selectionPoints = new List<GameObject>();
        symmetryAngles = new List<Quaternion>();
        for (int i = 0; i < SymmetryPointsCount; i++)
        {
            selectionPoints.Add(Instantiate(SelectionPointPrefab));
        }
        //selectionPoints.Add(Instantiate(SelectionPointPrefab));
        SetSymmetryAxis(symmetryAxis);
        ChangeSymmetryCount(SymmetryPointsCount, true);

        context.CameraController.EventsOnClick.AddListener(GenerateStreamSeedPointsAndStreamlines);
    }

    private void OnDestroy()
    {
        context.CameraController.EventsOnClick.RemoveListener(GenerateStreamSeedPointsAndStreamlines);
    }

    public void SetSymmetryAxis(SymmetryAxis axis)
    {
        switch (axis)
        {
            case SymmetryAxis.zAxis:
                Axis = new Vector3(0, 0, 1);
                break;
            case SymmetryAxis.xAxis:
                Axis = new Vector3(1, 0, 0);
                break;
            case SymmetryAxis.yAxis:
                Axis = new Vector3(0, 1, 0);
                break;
            case SymmetryAxis.planeNormal:
                Axis = crossSection.GetNormal();
                break;
            default:
                Axis = new Vector3(0, 0, 1);
                break;
        }
        symmetryAngles.Clear();
        for (int i = 0; i < SymmetryPointsCount; i++)
        {
            symmetryAngles.Add(Quaternion.AngleAxis((360.0f / SymmetryPointsCount) * i, Axis));
            Debug.Log("added");
        }
    }

    /// <summary>
    /// Changes the angles of all symmetry points and updates the game object list
    /// </summary>
    /// <param name="count"> new amount of symmetry points</param>
    public void ChangeSymmetryCount(int count, bool init = false)
    {
        if (count == SymmetryPointsCount && !init)
            return;
        SymmetryPointsCount = count;
        symmetryAngles.Clear();
        for (int i = 0; i < SymmetryPointsCount; i++)
        {
            symmetryAngles.Add(Quaternion.AngleAxis((360.0f / SymmetryPointsCount) * i, Axis));
            Debug.Log("added");
        }
        if (count > selectionPoints.Count) // add new points
        {
            int additionalPoints = count - selectionPoints.Count;
            for (int i = 0; i < additionalPoints; i++)
            {
                selectionPoints.Add(Instantiate(SelectionPointPrefab));
            }
        }
        else // remove points (ideally, we would just deactivate)
        {
            int removePoints = selectionPoints.Count - count;
            for (int i = count; i < selectionPoints.Count; i++)
            {
                Destroy(selectionPoints[i]);
            }
            selectionPoints.RemoveRange(count, removePoints);
        }

    }

    public void UpdateGOPosition(Vector3 mousePosition)
    {
        if (UseSymmetry)
        {
            for (int j = 0; j < SymmetryPointsCount; j++)
            {
                selectionPoints[j].transform.position = symmetryAngles[j] * mousePosition;
            }
        }
        else
        {
            selectionPoints[0].transform.position = symmetryAngles[0] * mousePosition;
        }
    }

    public void GenerateStreamSeedPointsAndStreamlines()
    {
        // TODO: Secure the case where mirrored points are not within the data range
        if (!this.gameObject.activeSelf || OnMouseOverHighlight.MODE != 0)
            return;
        List<Vector3> pointsToAdd = new List<Vector3>();
        foreach (var point in selectionPoints)
        {
            pointsToAdd.Add(point.transform.position);
            
        }
        StreamlineGenerator.AddManualSeeds(pointsToAdd);
        
    }


    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject() || OnMouseOverHighlight.MODE != 0)
        {
            if (isActive)
            {
                foreach (var point in selectionPoints)
                {
                    point.SetActive(false);
                }
                isActive = false;
            }

            return;
        }
        if (!isActive)
        {
            foreach (var point in selectionPoints)
            {
                point.SetActive(true);
            }
            isActive = true;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100))
        {
            UpdateGOPosition(hit.point);

            if (context.ControlHandler.IsMouseClicked(this.GetInstanceID()))
            {
                GenerateStreamSeedPointsAndStreamlines();
            }
        } 

    }
}
