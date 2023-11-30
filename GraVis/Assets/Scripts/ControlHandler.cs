using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ControlHandler : MonoBehaviour
{

    private int objectInUse;
    private int objectInFocus;
    private int objectClicked;

    private bool clickCatched;
    private int nullID; // ID marked as "no object", it's ID is this Controller, since it cannot be interacted with
    private bool focusSet;

    private int performanceNeed;

    public void Awake()
    {
        nullID = this.GetInstanceID();
        objectInUse = nullID;
        clickCatched = false;
        objectInFocus = nullID;
        focusSet = false;
        performanceNeed = 0;
    }

    public void RemoveUsage(int objectID)
    {
        if (objectInUse == objectID)
        {
            objectInUse = nullID;
        }
    }

    public void CatchClickedElment()
    {
        if (Input.GetMouseButtonUp(0) && !clickCatched)
        {
            objectClicked = objectInUse;
            objectInUse = nullID;
            clickCatched = true;
        }
    }

    public bool IsMouseClicked(int objectID, int mouseButton = 0, bool specialCase = false, bool specialCondition = true)
    {
        CatchClickedElment();
        if (IsOverUI())
            return false;
        if (Input.GetMouseButtonUp(mouseButton) && (objectClicked == nullID || objectClicked == objectID || specialCase) && specialCondition)
        {
            return true;
        }
        return false;
    }


    public bool IsMouseDragging(int objectID, int mouseButton = 0)
    {
        if (Input.GetMouseButtonDown(mouseButton))
        {
            //Debug.Log("Mouse down");
            if (IsOverUI())
                return false;
            if (objectInUse == nullID && (objectInFocus == nullID || objectInFocus == objectID))
            {
                objectInUse = objectID;
                return true;
            }
        }
        if (Input.GetMouseButton(mouseButton))
        {
            if (objectInUse == objectID)
            {
                return true;
            }
        }
        CatchClickedElment();
        return false;

    }

    public void SetFocus(int objectID, bool force = false)
    {
        
        if (objectInFocus == objectID || objectInFocus == nullID || force)
        {
            focusSet = true;
            objectInFocus = objectID;
        }
        
    }

    public void FreeFocus()
    {
        objectInFocus = nullID;
    }

    public bool IsOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    public bool IsApplicationFocused()
    {
        return Application.isFocused;
    }

    public int GetPerformanceNeed()
    {
        return performanceNeed;
    }

    public void SetPerformanceNeed(int need)
    {
        if (need > performanceNeed)
            performanceNeed = need;
    }

    public void ResetPerformanceNeed()
    {
        performanceNeed = 0;
    }

    /// <summary>
    /// Update of ControlHandler is called before the default update of other scripts
    /// </summary>
    public void Update()
    {
        // We first check if the mouse is pressed



        CatchClickedElment();
        if (!Input.GetMouseButtonUp(0))
        {
            clickCatched = false;
        }
        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
            objectInUse = nullID;

        if (!focusSet) // this assures that there is one frame between setting and releasing the focus
            FreeFocus();
        focusSet = false;
    }

    public void LateUpdate() // reset all things that are frame dependant
    {
        //performanceNeed = 0;
        ResetPerformanceNeed();
    }

}
