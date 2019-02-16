using UnityEngine;

namespace Pandawan.Islands.Camera
{
    /// <summary>
    ///     Allows the to camera follow a target
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        // TODO: Make better Camera movement using bounds and perhaps allow player to move around a bit before following?

        /// <summary>
        ///     The target to follow
        /// </summary>
        [SerializeField] private Transform target;

        private void Awake()
        {
            // TODO: Make auto-find target system
            if (target == null) Debug.LogError("No target set for CameraFollow");
        }

        private void Update()
        {
            if (target != null)
                transform.position = new Vector3(target.transform.position.x, target.transform.position.y,
                    transform.position.z);
        }
    }
}