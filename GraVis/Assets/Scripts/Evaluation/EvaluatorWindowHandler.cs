using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class EvaluatorWindowHandler : AbstractSingletonToolBehaviour<EvaluatorWindowHandler>
{
    Evaluator evaluator;
    public TMP_Text TaskTitle;
    public TMP_Text TaskDescription;
    public GameObject InputFieldGO;
    
    [HideInInspector]
    public TMP_InputField InputField;

    public bool Ready = false;

    public override void Init(ContextManager context)
    {
        if (deleteImmidiate)
            return;
        Context = context;
        evaluator = Context.Evaluator;
    }


    private void Awake()
    {
        Init(Context);
        InputField = InputFieldGO.GetComponent<TMP_InputField>();
    }

    // Start is called before the first frame update
    void Start()
    {
 
    }

    public void PressOK()
    {
        Debug.Log("Button pressed");
        Ready = true;
    }

    // Update is called once per frame
    void Update()
    {
        TaskTitle.SetText(evaluator.GetCurrentName());
        TaskDescription.SetText(evaluator.GetCurrentSubtaskName() + ": " +
            evaluator.GetCurrentDescription());
    }
}
