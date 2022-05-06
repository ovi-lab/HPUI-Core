using System;

namespace HPUI.Utils
{
    [Serializable]
    public class Range
    {
	public float min = 0.8f;
	public float max = 1.2f;

	public float getScaledValue(float scaleFactor)
	{
	    return (max - min) *  scaleFactor + min;
	}

	public float getInverseScaledValue(float scale)
	{
	    return (scale - min) / (max - min);
	}

	public void normalizeScaleOn(float normalizationValue)
	{
	    var mid = (min + max) / 2;
	    var halfRange = (max - min) / 2;
	    var normalizedHalfRange = (normalizationValue / mid) * halfRange;
	    min = mid - normalizedHalfRange;
	    max = mid + normalizedHalfRange;
	}
    }
}
