using System.Collections.Generic;

using UnityEngine;

namespace ArcadeVehicleController {
    /**
        Built upon original code by Kenney (www.kenney.nl)

        This controller uses a sphere to be the physical representation of the vehicle, with the model
        following its movements. Forward force is applied to the sphere in the direction the model faces.

        To set up the vehicle you need a structure such as this:
        Empty GameObject - provides vehicle's name and position
            -> Sphere with Rigidbody and Sphere Collider on "Ignore Raycast" layer
            -> GameObject with VehicleController and VehicleCameraController
                -> GameObject containing model (referenced by "container" variable in code), which is tilted into corners if tiltFactor is set
                    -> (optional) GameObject named "body", to be tilted by forces on turns and acceleration
                    -> (optional) GameObject name "wheelFrontLeft" and "wheelFrontRight", to turn when steering
            -> Empty GameObject to be the camera rig referenced by the VehicleCameraController
                -> GameObject with camera component

        To have collisions with the mesh you'll have to implement this with a trigger collider and apply forces to the sphere and the collider as appropriate.

        "onGround" and "nearGround" concepts are primarily used to affect the jump action, 
        allowing a held jump input to jump higher by disabling gravity whilst near ground on the way up.
        
        The 3 drag profiles, groundDrag, nearGroundDrag and airDrag, allow you to change vehicle behaviour depending on what's going on.
        eg, reduce the z-axis value in groundDrag to allow the vehicle to go faster whilst on the ground, 
        or increase the x-axis value in airDrag to make the vehicle perform sharper, less drifty-turns whilst airborne!
        
        By default there is no y-axis drag whilst air-borne, which causes the vehicle to drop more rapidly.

        Gravity for the vehicle is entirly controlled by this script, which applies a mulitplier to the physics gravity. 
        This allows for a vehicle's fall behaviour to be tuned or even modified dynamically as required.

        There are a number of "magic numbers" used, particularly for modifying the time delta when lerping between values. 
        Feel free to modify and tune these for the look and feel of your application.
    **/
    public class VehicleController : MonoBehaviour {
        [Header("Components")]
        public InputSource inputSource;
        public Transform vehicleModel;
        public Rigidbody sphere;

        [Header("Parameters")]

        [Tooltip("Acceleration force; speed will be a function of acceleration vs drag")]
        [Range(5.0f, 40.0f)] public float maxAcceleration = 30f;

        [Tooltip("Rate of turn; roughly degrees per second")]
        [Range(20.0f, 160.0f)] public float steering = 80f;

        [Tooltip("Upwards velocity applied when jumping")]
        [Range(10.0f, 20.0f)] public float jumpForce = 15f;

        [Tooltip("Multiplier on simulated gravity to allow tuning airtime")]
        [Range(0.0f, 10.0f)] public float gravityMultiplier = 1f;

        [Tooltip("Drag applied to vehicle motion in each axis when 'on ground'")]
        public Vector3 groundDrag = Vector3.one;

        [Tooltip("Drag applied to vehicle motion in each axis when 'near ground'")]
        public Vector3 nearGroundDrag = Vector3.one;

        [Tooltip("Drag applied to vehicle motion in each axis when not near ground")]
        public Vector3 airDrag = new Vector3(1, 0, 1);

        [Tooltip("Rate at which vehicle comes to rest")]
        public float stationaryDampenFactor = 4;

        [Tooltip("Amount to lean into a corner, correlated to steering value")]
        [Range(0.0f, 1.5f)] public float tiltFactor = 0.75f;

        [Tooltip("Offset of the vehicle within the rigidbody sphere")]
        [Range(-1.0f, 1.0f)] public float vehicleSphereOffset = 0;

        [Tooltip("Maximum height of vehicle above ground that is considered 'near ground'")]
        [Range(0f, 5.0f)] public float nearGroundThreshold = 2;

        [Tooltip("Maximum height of vehicle above ground that is considered 'on ground'")]
        [Range(0f, 5.0f)] public float onGroundThreshold = 1;

        [Header("Switches")]
        public bool jumpAbility = false;
        public bool steerInAir = true;

        // Vehicle components
        private Transform container, wheelFrontLeft, wheelFrontRight;
        private Transform body;

        // Private
        private float targetAccel, currentAccel;
        private float targetRotate, currentRotate;
        private bool nearGround, onGround;
        private bool intendToJump, isCompletingJump;

        private Vector3 containerBase;

        private void Awake() {
            var smokePositionsList = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>()) {
                switch (t.name) {
                    // Vehicle components
                    case "wheelFrontLeft": wheelFrontLeft = t; break;
                    case "wheelFrontRight": wheelFrontRight = t; break;
                    case "body": body = t; break;
                }
            }
            container = vehicleModel.GetChild(0);
            containerBase = container.localPosition;

            // these parameters are controlled by this script, so enforce that here to avoid weirdness
            sphere.useGravity = false;
            sphere.drag = 0;
        }

        private void Update() {
            // Wheel and body tilt
            if (wheelFrontLeft != null) { wheelFrontLeft.localRotation = Quaternion.Euler(0, currentRotate / 2, 0); }
            if (wheelFrontRight != null) { wheelFrontRight.localRotation = Quaternion.Euler(0, currentRotate / 2, 0); }

            body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(new Vector3(currentAccel / 4, 0, currentRotate / 6)), Time.deltaTime * 4);

            // Vehicle tilt
            float tilt;
            if (tiltFactor > 0) {
                tilt = -currentRotate * tiltFactor;
            } else {
                tilt = 0.0f;
            }

            container.localPosition = containerBase + new Vector3(0, Mathf.Abs(tilt) / 2000, 0);
            container.localRotation = Quaternion.Slerp(container.localRotation, Quaternion.Euler(0, currentRotate / 8, tilt), Time.deltaTime * 10);
        }

        private void FixedUpdate() {
            RaycastHit hitOn;
            RaycastHit hitNear;
            float dt = Time.fixedDeltaTime;

            onGround = Physics.Raycast(transform.position, Vector3.down, out hitOn, onGroundThreshold);
            nearGround = Physics.Raycast(transform.position, Vector3.down, out hitNear, nearGroundThreshold);

            processInput(inputSource);

            currentAccel = Mathf.SmoothStep(currentAccel, targetAccel, dt * 12);
            currentRotate = Mathf.Lerp(currentRotate, targetRotate, dt * 4);

            // Rotate around y for facing
            if (nearGround || steerInAir) {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(new Vector3(0, transform.eulerAngles.y + currentRotate, 0)), dt * 2);
            }

            // Align model normal to match transform
            if (nearGround) {
                // align model to ground surface
                vehicleModel.up = Vector3.Lerp(vehicleModel.up, hitNear.normal, dt * 8);
            } else {
                // gently bring model back to level whilst in the air
                vehicleModel.up = Vector3.Lerp(vehicleModel.up, Vector3.up, dt * 2);
            }
            vehicleModel.Rotate(0, transform.eulerAngles.y, 0);

            // Movement
            sphere.AddForce(transform.forward * currentAccel, ForceMode.Acceleration);

            // Jump action
            if (isCompletingJump) {
                if (onGround) {
                    isCompletingJump = false;
                }
            } else if (intendToJump) {
                if (!nearGround) {
                    isCompletingJump = true;
                } else if (onGround) {
                    sphere.AddForce(Vector3.up * jumpForce * 10 * dt, ForceMode.VelocityChange);
                }
            }

            // Simulated gravity
            if (isCompletingJump || !intendToJump) {
                sphere.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
            }

            // Move vehicle to track sphere position
            transform.position = sphere.transform.position + new Vector3(0, vehicleSphereOffset, 0);

            // Apply drag, depending on vehicle conditions
            if (nearGround) {
                applyDrag(groundDrag, dt);
            } else if (nearGround) {
                applyDrag(nearGroundDrag, dt);
            } else {
                applyDrag(airDrag, dt);
            }

            // Stops vehicle from floating around when standing still
            if (onGround
                && targetAccel == 0
                && sphere.velocity.magnitude < 4f
            ) {
                sphere.velocity = Vector3.Lerp(sphere.velocity, Vector3.zero, dt * stationaryDampenFactor);
            }
        }

        private void applyDrag(Vector3 drag, float dt) {
            Vector3 localVelocity = transform.InverseTransformVector(sphere.velocity);
            localVelocity.x = localVelocity.x * (1 - dt * drag.x);
            localVelocity.y = localVelocity.y * (1 - dt * drag.y);
            localVelocity.z = localVelocity.z * (1 - dt * drag.z);
            sphere.velocity = transform.TransformVector(localVelocity);
        }

        private void processInput(InputSource input) {
            var leftRight = input.moveXY().x;
            var upDown = input.moveXY().y;
            var isActionDown = input.isAction();

            targetAccel = upDown * maxAcceleration;
            targetRotate = leftRight * steering;
            intendToJump = jumpAbility && isActionDown;
        }

        public void SetPosition(Vector3 position, Quaternion rotation) {
            // Stop vehicle
            targetAccel = targetRotate = 0.0f;
            sphere.velocity = Vector3.zero;
            sphere.position = position;

            // Set new position
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}
