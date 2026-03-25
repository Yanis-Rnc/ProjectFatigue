using UnityEngine;
using UnityEngine.XR;

namespace Controls
{
    public class ModeManager : MonoBehaviour
    {
        public GameObject xrOrigin;
        public GameObject desktopPlayer;
        public Camera xrCamera;
        public Camera desktopCamera;
        public FittsLaw.FittsExperimentManager experimentManager;
        
        void Start()
        {
            if (!XRSettings.isDeviceActive)
            {
                xrOrigin.SetActive(false);
                desktopPlayer.SetActive(true);

                experimentManager.playerCamera = desktopCamera;
            }
            else
            {
                xrOrigin.SetActive(true);
                desktopPlayer.SetActive(false);

                experimentManager.playerCamera = xrCamera;
            }
        }
    }
}