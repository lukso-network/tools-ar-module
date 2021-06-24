using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.CodeDom;
using System;
using System.Dynamic;
using System.Linq;

public class StatisticDisplay : MonoBehaviour
{

    // Attach this to a Text component

    public float updateInterval = 0.5F;
    public string format = "{0:F2}";
    public Func<float, Color> SetColor;

    private float[] accumulatedValue;
    public string[] titles; 
    private int count = 0;
    private float timeleft; // Left time for current interval
    private Text displayText;
    private string message;

    void Start()
    {
        accumulatedValue = new float[titles.Length];
        displayText = GetComponent<Text>();
        if (displayText == null)
        {
            Debug.Log("StatisticDisplay needs a Text component!");
            enabled = false;
            return;
        }
        timeleft = updateInterval;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;

        if (timeleft <= 0.0)
        {
            timeleft = updateInterval;
            if (count > 0)
            {



                String text = String.Join("\n", 
                    accumulatedValue.Select(x => x / count * 1000)
                    .Zip(titles, (val, str)=> String.Format("{0}: {1:0.##}", str, val))
                    .ToArray());

                //float valueToDisplay = accumulatedValue / count;
                //string formatText = System.String.Format(format, valueToDisplay);
                //if (SetColor != null)
                  //  displayText.color = SetColor(valueToDisplay);
                displayText.text = message + "*\n" +  text;
            }
            ResetValue();
        }
    }

    private void ResetValue()
    {
        Array.Clear(accumulatedValue, 0, accumulatedValue.Length);
        count = 0;
    }

    public void LogValue(string message, params float [] value)
    {

        this.message =  message;
        var len = Math.Min(value.Length, accumulatedValue.Length);
        for (int i = 0; i < len; ++i) {
            accumulatedValue[i] += value[i];
        }

        count++;
    }
}