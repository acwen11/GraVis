using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class RangedSliderHandler : MonoBehaviour
{
    public float Min, Max;
    public bool WholeNumbers;
    public float Value;

    [Space]
    public UnityEvent OnChange;

    [Space]
    public TMP_Text Text;
    public Slider slider;
    public TMP_InputField InField;

    private void Start()
    {
        Init(Value, Min, Max, WholeNumbers);
    }

    public void Init(float value, float min, float max, bool wholeNumbers = false)
    {
        Value = Mathf.Clamp(value, min, max);
        slider.wholeNumbers = wholeNumbers;
        WholeNumbers = wholeNumbers;
        if (wholeNumbers)
        {
            int ival = (int)Value;
            slider.minValue = (int)min;
            slider.maxValue = (int)max;
            slider.value = (int)Value;
            InField.characterValidation = TMP_InputField.CharacterValidation.Integer;
            InField.SetTextWithoutNotify(ival.ToString());
        }
        else
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = Value;
            InField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            InField.SetTextWithoutNotify(Value.ToString("F2"));
        }
        Min = min;
        Max = max;
    }

    public void SetValue(float inValue)
    {
        slider.SetValueWithoutNotify(inValue);
        InField.SetTextWithoutNotify(inValue.ToString("F2"));
        Value = slider.value;
        OnChange.Invoke();
    }

    public void SetValue(int inValue)
    {
        slider.SetValueWithoutNotify(inValue);
        InField.SetTextWithoutNotify(inValue.ToString());
        Value = slider.value;
        OnChange.Invoke();
    }

    public void InFieldValueChange()
    {
        if (WholeNumbers)
        {
            int value = int.Parse(InField.text);
            value = (int) Mathf.Clamp(value, Min, Max);
            slider.SetValueWithoutNotify(value);
            InField.SetTextWithoutNotify(value.ToString());
        }else
        {
            float value = float.Parse(InField.text);
            value = Mathf.Clamp(value, Min, Max);
            slider.SetValueWithoutNotify(value);
            InField.SetTextWithoutNotify(value.ToString("F2"));
        }
        Value = slider.value;
        OnChange.Invoke();
    }

    public void SliderValueChange()
    {
        if (WholeNumbers)
        {
            int value = (int)slider.value;
            InField.SetTextWithoutNotify(((int)value).ToString());
        }
        else
        {
            float value = slider.value;
            InField.SetTextWithoutNotify(value.ToString("F2"));
        }
        Value = slider.value;
        OnChange.Invoke();
    }

}
