using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dependency
{
    private bool isOpen;

    public Dependency()
    {
        isOpen = false;
    }

    public void Open()
    {
        isOpen = true;
    }
    public void Close()
    {
        isOpen = false;
    }

    public bool isClosed()
    {
        return !isOpen;
    }

    public bool IsOpen()
    {
        return isOpen;
    }



}
