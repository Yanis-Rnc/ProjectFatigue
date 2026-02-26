using UnityEngine;
using UnityEngine.XR;

namespace Controls
{
    public class ModeManager : MonoBehaviour
    {
        public GameObject xrOrigin;
        public GameObject desktopPlayer;

        void Start()
        {
            if (!XRSettings.isDeviceActive)
            {
                xrOrigin.SetActive(false);
                desktopPlayer.SetActive(true);
            }
            else
            {
                xrOrigin.SetActive(true);
                desktopPlayer.SetActive(false);
            }
        }
    }
}