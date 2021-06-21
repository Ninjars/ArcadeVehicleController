using UnityEngine;

namespace ArcadeVehicleController {
    public class DustEmitter : MonoBehaviour {
        public ParticleSystem dustTrailPrefab;

        [Tooltip("Rigidbody driving the object this emitter is attached to")]
        public Rigidbody referenceRigidbody;

        [Tooltip("Maximum distance to ground at which dust is produced")]
        [Range(0f, 5.0f)] public float maxHeight = 1.5f;

        [Tooltip("Minimum speed at which dust is produced")]
        public float minSpeed = 8;

        [Tooltip("Emit no matter the direction of travel, or only when drifting")]
        public bool alwaysEmit = true;

        private ParticleSystem dust;

        private void Awake() {
            dust = Instantiate(dustTrailPrefab, transform.position, transform.rotation, transform);
            ParticleSystem.EmissionModule emissionModule = dust.emission;
            emissionModule.enabled = false;
        }

        private void Update() {
            RaycastHit hitOn;
            var enableSmoke = Physics.Raycast(transform.position, Vector3.down, out hitOn, maxHeight);

            if (enableSmoke) {
                dust.transform.position = hitOn.point;
            } else if (Physics.Raycast(transform.position, Vector3.down, out hitOn, 100f)) {
                dust.transform.position = hitOn.point;
            } else {
                dust.transform.position = transform.position - Vector3.up * 100f;
            }
            ParticleSystem.EmissionModule smokeEmission = dust.emission;
            smokeEmission.enabled = enableSmoke 
                && referenceRigidbody.velocity.magnitude > minSpeed 
                && (alwaysEmit || Vector3.Angle(referenceRigidbody.velocity, transform.forward) > 30.0f);
        }
    }
}
