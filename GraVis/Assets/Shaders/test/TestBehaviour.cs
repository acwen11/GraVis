using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TestBehaviour : MonoBehaviour
{

    public Material mat;
    public float speed = 0.5f;
    

    private float t = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        mat.SetFloat("_t", t);
        Texture2D randomImg = new Texture2D(256,256, TextureFormat.ARGB32, false);
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 256; j++)
            {
                int value = Random.Range(0, 256);
                if (value < 220)
                    value = 0;

                randomImg.SetPixel(i, j, new Color(value, value, value, 256));
            }
        }
        mat.SetTexture("_Randomfield", randomImg);
    }

    // Update is called once per frame
    
    void Update()
    {
        mat.SetFloat("_t", t);
        t += speed;
    }
}
