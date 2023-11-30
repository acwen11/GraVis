using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossSectionToolHandler : AbstractSingletonToolBehaviour<CrossSectionToolHandler>
{

    public void Start()
    {
    }

    public override void Init(ContextManager Context)
    {
        base.Init(Context);
        Context.CrossSection.gameObject.SetActive(true);
    }

    public void ToRotationMode()
    {
        OnMouseOverHighlight.MODE = 2;
    }
    public void ToTranslationMode()
    {
        OnMouseOverHighlight.MODE = 1;
    }
    public void ToNoneMode()
    {
        OnMouseOverHighlight.MODE = 0;
    }

    public void ResetPlane()
    {
        Context.CrossSection.transform.position = Vector3.zero;
        Context.CrossSection.transform.rotation = Quaternion.identity;
    }

    public void OnDestroy()
    {
        if (deleteImmidiate)
            return;
        Context?.CrossSection?.gameObject.SetActive(false);
    }
}
