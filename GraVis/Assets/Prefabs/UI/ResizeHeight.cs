using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResizeHeight : MonoBehaviour
{


    // Update is called once per frame
    void Update()
    {
        float size = 10; // Padding
        for (int i = 0; i < transform.childCount; i++)
        {
            size += transform.GetChild(i).GetComponent<RectTransform>().rect.height + 5; // Spacing;
        }

        GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
    }
}
