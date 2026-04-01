using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void StartCalibration()
    {
        SceneManager.LoadScene("CalibrationScene", LoadSceneMode.Single);
    }
}