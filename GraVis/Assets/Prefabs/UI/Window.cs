using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Window : MonoBehaviour, IDragHandler
{
    public bool IsClosable;
    public float Width;
    public float Height;
    public bool isResizable;
    public bool IsMovable;

    public string WindowTitle;
    public TMPro.TextMeshProUGUI TitleText;

    private GameObject Container;

    private bool isMinimized;

    public void SetTitle(string title)
    {
        TitleText.SetText(title);
        WindowTitle = title;
    }

    public void Start()
    {
        SetTitle(WindowTitle);
        isMinimized = false;
        
        Container = gameObject.transform.Find("Container").gameObject;
        
    }

    public void Close()
    {
        Object.Destroy(gameObject);
    }

    private void ResetSize()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        float size = 50;
        for (int i = 0; i < Container.transform.childCount; i++)
            size += Container.transform.GetChild(i).GetComponent<RectTransform>().rect.height + 10; // Spacing;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
    }

    public void MinMaximize()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (isMinimized)
        {
            Container.SetActive(true);
            ResetSize();
            isMinimized = false;
        }
        else
        {
            Container.SetActive(false);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);
            isMinimized = true;
        }
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void Update()
    {
        if (isResizable)
            ChangeSize(Width, Height);
    }

    public void ChangeSize(float sizeX, float sizeY)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizeX);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizeY);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsMovable)
            transform.transform.position = eventData.position;
        
    }
}
