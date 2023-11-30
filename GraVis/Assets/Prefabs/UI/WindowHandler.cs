using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum WindowOpenState
{
    Open,
    Collapsed
}

public class WindowHandler : MonoBehaviour
{
    public string Name;

    [Space]
    public TMP_Text Title;
    public GameObject CloseButton;
    public GameObject CollapseButton;
    public GameObject Container;
    public GameObject UpperBar;

    private WindowOpenState openState;
    private RectTransform rectTransform;
    private float standardHeight;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        Title.SetText(Name);

        openState = WindowOpenState.Open;
        

        VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
        standardHeight = layout.padding.top + layout.padding.bottom;
        standardHeight += UpperBar.GetComponent<RectTransform>().rect.height;
        
        SetOpenSize();
    }

    void Update()
    {
        
    }

    public void SetOpenSize()
    {
        float containerSize = 0;
        var layout = Container.GetComponent<VerticalLayoutGroup>();
        for (int i = 0; i < Container.transform.childCount; i++)
        {
            if (Container.transform.GetChild(i).gameObject.activeSelf) // Exclude inactive childs
                containerSize += Container.transform.GetChild(i).GetComponent<RectTransform>().rect.height;
        }
        containerSize += layout.spacing * Mathf.Max(0, Container.transform.childCount - 1);
        if (Container.transform.childCount > 0)
            containerSize += layout.padding.top + layout.padding.bottom;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, containerSize + standardHeight);
    }

    public void Collapse()
    {
        switch (openState)
        {
            case WindowOpenState.Collapsed:
                Container.SetActive(true);
                SetOpenSize();
                openState = WindowOpenState.Open;
                break;
            case WindowOpenState.Open:
                Container.SetActive(false);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, standardHeight);
                openState = WindowOpenState.Collapsed;
                break;
            default:


                break;
        }

    }

    public void Close()
    {
        Destroy(this.gameObject);
    }
}
