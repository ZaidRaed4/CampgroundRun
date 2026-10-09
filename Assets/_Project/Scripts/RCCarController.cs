using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]

public class RCCarController : MonoBehaviour
{
    private enum DriveType { RearWheel, AllWheel }

    [Header("Drive")]
    [SerializeField] private DriveType driveType = DriveType.AllWheel;


    [Header("Wheel Collider")]
    [SerializeField] private WheelCollider frontLeft;
    [SerializeField] private WheelCollider frontRight;
    [SerializeField] private WheelCollider rearLeft;
    [SerializeField] private WheelCollider rearRight;

    [Header("Wheel Meshes")]
    [SerializeField] private Transform frontLeftMesh;
    [SerializeField] private Transform frontRightMesh;
    [SerializeField] private Transform rearLeftMesh;
    [SerializeField] private Transform rearRightMesh;


    [Header("Torque (N.m per wheel)")]
    [SerializeField] private float maxMotorTorque = 0.12f;
    [SerializeField] private float maxBrakeTorque = 0.12f;
    [SerializeField] private float handBrakeTorque = 0.06f;
    [SerializeField] private float coastBrakeTorque = 0.01f;


    [Header("Limits")]
    [SerializeField] private float maxForwardSpeed = 15f;
    [SerializeField] private float maxReverseSpeed = 4f;


    [Header("Steering")]
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float highSpeedSteerAngle = 6f;
    [SerializeField] private float steerRate = 180f;


    [Header("Handbrake")]
    [SerializeField] private float handBrakeRearGrip = 0.65f;
    [SerializeField] private float rearGripChangeRate = 2f;


    [Header("Anti-Roll")]
    [SerializeField] private float antiRollForce = 5f;


    private Rigidbody rb;

    private InputAction throttleAction;
    private InputAction steerAction;
    private InputAction handBrakeAction;

    private float throttleInput;
    private float steerInput;
    private bool handBrakeInput;

    private float currentSteerAngle;
    private float rearSidewaysStiffness;
    private float currentRearGrip = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        throttleAction = InputSystem.actions.FindAction("Vehicle/Throttle", throwIfNotFound: true);
        steerAction = InputSystem.actions.FindAction("Vehicle/Steer", throwIfNotFound: true);
        handBrakeAction = InputSystem.actions.FindAction("Vehicle/Handbrake", throwIfNotFound: true);

        rearSidewaysStiffness = rearLeft.sidewaysFriction.stiffness;
        frontLeft.ConfigureVehicleSubsteps(5f, 12, 15);
    }

    private void Update()
    {
        throttleInput = throttleAction.ReadValue<float>();
        steerInput = steerAction.ReadValue<float>();
        handBrakeInput = handBrakeAction.IsPressed();

        SyncWheelMesh(frontLeft, frontLeftMesh);
        SyncWheelMesh(frontRight, frontRightMesh);
        SyncWheelMesh(rearLeft, rearLeftMesh);
        SyncWheelMesh(rearRight, rearRightMesh);
    }

    private void FixedUpdate()
    {
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        Steer(forwardSpeed);
        DriveAndBrake(forwardSpeed);
        ApplyHandBrakeGrip();
        ApplyAntiRoll(frontLeft, frontRight);
        ApplyAntiRoll(rearLeft, rearRight);
    }

    private void Steer(float forwardSpeed)
    {
        float speedFactor = Mathf.InverseLerp(0f, maxForwardSpeed, Mathf.Abs(forwardSpeed));
        float angleLimit = Mathf.Lerp(maxSteerAngle, highSpeedSteerAngle, speedFactor);
        float targetAngle = steerInput * angleLimit;

        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetAngle, steerRate * Time.fixedDeltaTime);
        frontLeft.steerAngle = currentSteerAngle;
        frontRight.steerAngle = currentSteerAngle;
    }

    private void DriveAndBrake(float forwardSpeed)
    {
        bool hasThrottle = Mathf.Abs(throttleInput) > 0.01f;
        bool pressingAgainstMotion = throttleInput * forwardSpeed < 0f && Mathf.Abs(forwardSpeed) > 0.5f;

        float motor = 0f;
        float brake = 0f;

        if (!hasThrottle)
            brake = coastBrakeTorque;
        else if (pressingAgainstMotion)
            brake = Mathf.Abs(throttleInput) * maxBrakeTorque;
        else if (!IsOverSpeed(forwardSpeed))
            motor = throttleInput * maxMotorTorque;

        float frontMotor = driveType == DriveType.AllWheel ? motor : 0f;
        float rearMotor = handBrakeInput ? 0f : motor;
        frontLeft.motorTorque = frontMotor;
        frontRight.motorTorque = frontMotor;
        rearLeft.motorTorque = rearMotor;
        rearRight.motorTorque = rearMotor;

        float rearBrake = handBrakeInput ? handBrakeTorque : brake;
        frontLeft.brakeTorque = brake;
        frontRight.brakeTorque = brake;
        rearLeft.brakeTorque = rearBrake;
        rearRight.brakeTorque = rearBrake;
    }

    private bool IsOverSpeed(float forwardSpeed) =>
        throttleInput > 0f ? forwardSpeed > maxForwardSpeed : -forwardSpeed > maxReverseSpeed;

    private void ApplyHandBrakeGrip()
    {
        float targetGrip = handBrakeInput ? handBrakeRearGrip : 1f;
        currentRearGrip = Mathf.MoveTowards(currentRearGrip, targetGrip, rearGripChangeRate * Time.fixedDeltaTime);

        float stiffness = rearSidewaysStiffness * currentRearGrip;
        SetSidewaysStiffness(rearLeft, stiffness);
        SetSidewaysStiffness(rearRight, stiffness);
    }

    private static void SetSidewaysStiffness(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve friction = wheel.sidewaysFriction;
        friction.stiffness = stiffness;
        wheel.sidewaysFriction = friction;
    }

    private void ApplyAntiRoll(WheelCollider left, WheelCollider right)
    {
        bool leftGrounded = left.GetGroundHit(out WheelHit leftHit);
        bool rightGrounded = right.GetGroundHit(out WheelHit rightHit);

        float leftTravel = leftGrounded ? SuspensionTravel(left, leftHit) : 1f;
        float rightTravel = rightGrounded ? SuspensionTravel(right, rightHit) : 1f;

        float force = (leftTravel - rightTravel) * antiRollForce;

        if (leftGrounded)
            rb.AddForceAtPosition(left.transform.up * -force, left.transform.position);
        if (rightGrounded)
            rb.AddForceAtPosition(right.transform.up * force, right.transform.position);
    }

    private static float SuspensionTravel(WheelCollider wheel, WheelHit hit)
    {
        float wheelCenterDrop = -wheel.transform.InverseTransformPoint(hit.point).y - wheel.radius;
        return wheelCenterDrop / wheel.suspensionDistance;
    }

    private static void SyncWheelMesh(WheelCollider wheel, Transform mesh)
    {
        wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
        mesh.SetPositionAndRotation(position, rotation);
    }
}
