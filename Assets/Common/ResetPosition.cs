using UnityEngine;

namespace Common
{
    public class ResetPosition : MonoBehaviour
    {
        [SerializeField] private float treshold;
        [SerializeField] private Transform target;

        private void Update()
        {
            if (target.position.y < treshold)
                target.position = transform.position;
        }
    }
}