using UnityEngine;

namespace FittsLaw
{
    public class FittsTarget : MonoBehaviour
    {
        private FittsExperimentManager _manager;

        public void Init(FittsExperimentManager manager)
        {
            _manager = manager;
        }

        public void OnTargetHit()
        {
            _manager.TargetHit(this);
            _manager.RegisterShot(true);
        }
    }
}