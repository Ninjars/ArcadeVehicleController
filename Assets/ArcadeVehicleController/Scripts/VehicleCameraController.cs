using UnityEngine;

namespace ArcadeVehicleController {
    public class VehicleCameraController : MonoBehaviour {
        [Header("Components")]
        public Transform rig;

        [Range(1, 20)] public float followSpeed = 16;
        [Range(1, 20)] public float rotationSpeed = 12;

        public bool followRotation = true;

        // Private
        private Vector3 cameraPositionOffset;
        private Vector3 cameraRotationOffset;
        private Camera vehicleCamera;

        private void Awake() {
            // Store camera rig offsets
            cameraPositionOffset = rig.localPosition;
            cameraRotationOffset = rig.localEulerAngles;

            vehicleCamera = rig.GetChild(0).GetComponent<Camera>();
        }

        private void FixedUpdate() {
            // Camera follow
            var dt = Time.fixedDeltaTime;
            rig.position = Vector3.Lerp(rig.position, transform.position + cameraPositionOffset, dt * followSpeed);
            if (followRotation) { rig.rotation = Quaternion.Lerp(rig.rotation, Quaternion.Euler(transform.eulerAngles + cameraRotationOffset), dt * rotationSpeed); }
        }
    }
}
