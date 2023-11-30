using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadPlane : MonoBehaviour
{
    public Plane plane;
    private Vector3 Down;
    private Vector3 Right;

    // Start is called before the first frame update
    void Start()
    {
        ResetValues();
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            ResetValues();
            transform.hasChanged = false;
        }
    }

    public Vector4 GetNormalAndDistance()
    {
        return new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
    }

    public Vector3 GetRight()
    {
        return Right;
    }
    public Vector3 GetDown()
    {
        return Down;
    }


    public void ResetValues()
    {
        Down = transform.rotation * Vector3.down * transform.localScale.x * 0.5f;
        Right = transform.rotation * Vector3.right * transform.localScale.y * 0.5f;
        plane.Set3Points(transform.position, transform.position + Right, transform.position + Down);
    }

}
