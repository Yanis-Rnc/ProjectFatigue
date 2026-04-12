using UnityEngine;

public static class CalibrationData
{
    public static Vector3[] CalibrationPoints = new Vector3[6];
    public static Vector3 Center = Vector3.zero;
    public static float Radius = 0.3f;
    
    public static void Compute()
    {
        Center = (CalibrationPoints[0] + CalibrationPoints[1]) / 2f;

        float distSum = 0f;
        foreach (var p in CalibrationPoints)
            distSum += Vector3.Distance(Center, p);
        Radius = (distSum / CalibrationPoints.Length);
    }
}