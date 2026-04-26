using UnityEngine;

namespace FittsLaw
{
    /// <summary>
    /// Represents an interactable target in the Fitts' Law experiment.
    /// Notifies the experiment manager when hit.
    /// </summary>
    public class FittsTarget : MonoBehaviour
    {
        private FittsExperimentManager _manager;

        /// <summary>
        /// Initializes the target with a reference to the experiment manager.
        /// </summary>
        public void Init(FittsExperimentManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Called when the target is hit by the user.
        /// </summary>
        public void OnTargetHit()
        {
            _manager.TargetHit(this);
            _manager.RegisterShot(true);
        }
    }
}