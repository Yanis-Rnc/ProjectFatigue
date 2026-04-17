using UnityEngine;

public static class CalibrationData
{
    public static Vector3[] CalibrationPoints = new Vector3[6];
    public static Vector3 Center = Vector3.zero;

    public static float RadiusDepth  = 0.3f;
    public static float RadiusWidth  = 0.3f;
    public static float RadiusHeight = 0.3f;

    public static void Compute()
    {
        Center = (CalibrationPoints[0] + CalibrationPoints[1]) / 2f;

        RadiusDepth  = (Vector3.Distance(Center, CalibrationPoints[0]) + Vector3.Distance(Center, CalibrationPoints[1])) / 2f;
        RadiusWidth  = (Vector3.Distance(Center, CalibrationPoints[2]) + Vector3.Distance(Center, CalibrationPoints[3])) / 2.5f;
        RadiusHeight = (Vector3.Distance(Center, CalibrationPoints[4]) + Vector3.Distance(Center, CalibrationPoints[5])) / 2.5f;
    }
}