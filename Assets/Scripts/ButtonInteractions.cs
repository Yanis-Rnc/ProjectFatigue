using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void StartExperiment()
    {
        SceneManager.LoadScene("ExperimentScene", LoadSceneMode.Single);
    }

    public void StartCalibration()
    {
        SceneManager.LoadScene("CalibrationScene", LoadSceneMode.Single);
    }
}