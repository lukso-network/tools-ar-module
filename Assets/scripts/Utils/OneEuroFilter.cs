/* 
 * OneEuroFilter.cs
 * Author: Dario Mazzanti (dario.mazzanti@iit.it), 2016
 * 
 * This Unity C# utility is based on the C++ implementation of the OneEuroFilter algorithm by Nicolas Roussel (http://www.lifl.fr/~casiez/1euro/OneEuroFilter.cc)
 * More info on the 1€ filter by Géry Casiez at http://www.lifl.fr/~casiez/1euro/
 *
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

class LowPassFilter {
    float y, a, s;
    bool initialized;

    public void setAlpha(float _alpha) {
        if (_alpha <= 0.0f || _alpha > 1.0f) {
            Debug.LogError("alpha should be in (0.0., 1.0]");
            return;
        }
        a = _alpha;
    }

    public LowPassFilter(float _alpha, float _initval = 0.0f) {
        y = s = _initval;
        setAlpha(_alpha);
        initialized = false;
    }

    public float Filter(float _value) {
        float result;
        if (initialized)
            result = a * _value + (1.0f - a) * s;
        else {
            result = _value;
            initialized = true;
        }
        y = _value;
        s = result;
        return result;
    }

    public float filterWithAlpha(float _value, float _alpha) {
        setAlpha(_alpha);
        return Filter(_value);
    }

    public bool hasLastRawValue() {
        return initialized;
    }

    public float lastRawValue() {
        return y;
    }

};

// -----------------------------------------------------------------

[Serializable]
public class OneEuroFilterParams {

    //ange(0, 30)]
    //ublic float freq = 30;

    [Range(0, 5)]
    public float mincutoff = 1;
    [Range(0, 30)]
    public float beta = 0;
    [Range(0, 1)]
    public float dcutoff = 1;

    [HideInInspector]
    public float movementFactor = 1;
}

public class OneEuroFilter {
    float freq = 30;
    /*float mincutoff;
	float beta;
	float dcutoff;*/

    OneEuroFilterParams filterParams;
    LowPassFilter x;
    LowPassFilter dx;
    float lasttime;

    // currValue contains the latest value which have been succesfully filtered
    // prevValue contains the previous filtered value
    public float currValue { get; protected set; }
    public float prevValue { get; protected set; }

    float alpha(float _cutoff) {
        float te = 1.0f / freq;
        float tau = 1.0f / (2.0f * Mathf.PI * _cutoff);
        return 1.0f / (1.0f + tau / te);
    }

    public OneEuroFilter(OneEuroFilterParams settings) {
        this.filterParams = settings;

        x = new LowPassFilter(alpha(filterParams.mincutoff));
        dx = new LowPassFilter(alpha(filterParams.dcutoff));
        lasttime = -1.0f;

        currValue = 0.0f;
        prevValue = currValue;
    }

    public float Filter(float value, float timestamp, float presenceFactor = 1) {
        prevValue = currValue;

        if (timestamp < 0) {
            timestamp = Time.realtimeSinceStartup;
        }

        // update the sampling frequency based on timestamps
        if (lasttime != -1.0f)
            freq = 1.0f / (timestamp - lasttime);
        lasttime = timestamp;
        // estimate the current variation per second 
        float dvalue = x.hasLastRawValue() ? (value - x.lastRawValue()) * freq : 0.0f; // FIXME: 0.0 or value? 
        float edvalue = dx.filterWithAlpha(dvalue, alpha(filterParams.dcutoff));
        // use it to update the cutoff frequency
        float cutoff = (filterParams.mincutoff + filterParams.beta * Mathf.Abs(edvalue)) * filterParams.movementFactor * presenceFactor;
        // filter the given value
        currValue = x.filterWithAlpha(value, alpha(cutoff));

        return currValue;
    }
};

public class OneEuroFilter3D {
    private OneEuroFilter[] filters = new OneEuroFilter[3];
    public OneEuroFilter3D(OneEuroFilterParams settings) {
        for (int i = 0; i < filters.Length; ++i) {
            filters[i] = new OneEuroFilter(settings);
        }
    }

    public Vector3 Filter(Vector3 value, float timestamp, float presenceFactor = 1) {
        //values = filters.Select((x,i)=>filters[i].Filter(value[i], timestamp, presenceFactor));
        value.x = filters[0].Filter(value[0], timestamp, presenceFactor);
        value.y = filters[1].Filter(value[1], timestamp, presenceFactor);
        value.z = filters[2].Filter(value[2], timestamp, presenceFactor);
        return value;
    }
}


