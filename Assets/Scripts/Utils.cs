using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyUtils {

    public static float Map(float value, float inputMin, float inputMax, float outputMin, float outputMax, bool clamp)
    {

        if (Mathf.Abs(inputMin - inputMax) < 0.000001f)
        {
            return outputMin;
        }
        else
        {
            float outVal = ((value - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin);

            if (clamp)
            {
                if (outputMax < outputMin)
                {
                    if (outVal < outputMax) outVal = outputMax;
                    else if (outVal > outputMin) outVal = outputMin;
                }
                else
                {
                    if (outVal > outputMax) outVal = outputMax;
                    else if (outVal < outputMin) outVal = outputMin;
                }
            }
            return outVal;
        }
    }
}
