using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles navigation from the main menu.
/// </summary>
public class MenuManager : MonoBehaviour
{
    /// <summary>
    /// Loads the calibration scene.
    /// </summary>
    public void StartCalibration()
    {
        SceneManager.LoadScene("CalibrationScene", LoadSceneMode.Single);
    }
}