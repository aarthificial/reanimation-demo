using UnityEngine;

namespace Common
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class CameraConfiner : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private new Camera camera;
        [SerializeField] private float smoothing = 1;
        [SerializeField] private Vector2 deadZone;
        private BoxCollider2D _boundingBox;

        private void Awake()
        {
            _boundingBox = GetComponent<BoxCollider2D>();
        }

        private void LateUpdate()
        {
            var cameraPosition = camera.transform.position;

            Vector2 targetPosition = target.transform.position;
            var nextPosition = GetConfinedPoint(
                cameraPosition,
                targetPosition - deadZone,
                targetPosition + deadZone,
                Vector2.zero
            ); 
            
            var bounds = _boundingBox.bounds;
            var cameraOffset = new Vector3(
                camera.orthographicSize * camera.aspect,
                camera.orthographicSize
            );
            
            Vector3 confinedPosition = GetConfinedPoint(nextPosition, bounds.min, bounds.max, cameraOffset);
            confinedPosition.z = cameraPosition.z;

            camera.transform.position = Vector3.Lerp(
                cameraPosition,
                confinedPosition,
                Time.deltaTime * smoothing
            );
        }

        private Vector2 GetConfinedPoint(Vector2 point, Vector2 min, Vector2 max, Vector2 offset)
        {
            var cameraMin = point - offset;
            var cameraMax = point + offset;

            if (cameraMin.x < min.x)
                point.x = min.x + offset.x;
            else if (cameraMax.x > max.x)
                point.x = max.x - offset.x;

            if (cameraMin.y < min.y)
                point.y = min.y + offset.y;
            else if (cameraMax.y > max.y)
                point.y = max.y - offset.y;

            return point;
        }
    }
}