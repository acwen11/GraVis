using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawWorldPosition : MonoBehaviour
{
    
    public CrossSection section; // needed to cut transparency

    public Material worldPosMaterial;
    private Material alphaWorldPosMaterial;
    private Material[] tempMats;

    public List<GameObject> ObjectsToDraw;

    public void Start()
    {
        tempMats = new Material[ObjectsToDraw.Count];
        alphaWorldPosMaterial = new Material(worldPosMaterial);
        alphaWorldPosMaterial.name = "Alpha-Distance";
        alphaWorldPosMaterial.SetFloat("_DrawAlpha", 1.0f);
        worldPosMaterial.SetFloat("_DrawAlpha", 0.0f);
    }

    private void OnPreRender()
    {
        
        // Draw plane transparent
        alphaWorldPosMaterial.SetTexture("_MainTex", section.GetRenderTexture());
        
        for (int i= 0; i < ObjectsToDraw.Count; i++)
        {
            tempMats[i] = ObjectsToDraw[i].GetComponent<Renderer>().material;
            
            if (i == 0)
            {
                ObjectsToDraw[i].GetComponent<Renderer>().material = alphaWorldPosMaterial;
            }
            else
            {
                ObjectsToDraw[i].GetComponent<Renderer>().material = worldPosMaterial;
            }

        }
    }

    private void OnPostRender()
    {
        for (int i = 0; i < ObjectsToDraw.Count; i++)
        {
            ObjectsToDraw[i].GetComponent<Renderer>().material = tempMats[i];
        }
    }
}
