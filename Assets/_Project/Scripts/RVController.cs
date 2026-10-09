using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]

public class RVcontroller : MonoBehaviour
{
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
    [SerializeField] private float maxMotorTorque = 2500f;
    [SerializeField] private float maxBrakeTorque = 4000f;
    [SerializeField] private float handBrakeTorque = 8000f;
    [SerializeField] private float coastBrakeToque = 200f;


    [Header("Limits")]
    [SerializeField] private float maxForwardSpeed = 25f;
    [SerializeField] private float maxReverseSpeed = 6f;
    [SerializeField] private float maxSteerAngle = 30f;


    private Rigidbody rb;

    private InputAction throttleAction;
    private InputAction steerAction;
    private InputAction handBrakeAction;

    private float throttleInput;
    private float steerInput;
    private bool handBrakeInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        throttleAction = InputSystem.actions.FindAction("Vehicle/Throttle");
        steerAction = InputSystem.actions.FindAction("Vehicle/Steer");
        handBrakeAction = InputSystem.actions.FindAction("Vehicle/HandBrake");

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
        Steer();
        DriveAndBreake(forwardSpeed);
    }

    private void Steer()
    {
        float angle = steerInput * maxSteerAngle;
        frontLeft.steerAngle = angle;
        frontRight.steerAngle = angle;
    }

    private void DriveAndBreake(float forwardSpeed)
    {
        bool hasThrottle = Mathf.Abs(throttleInput) > 0.01f;
        bool pressingAgainstMotion = throttleInput * forwardSpeed < 0f && Mathf.Abs(forwardSpeed) > 0.05f;

        float motor = 0f;
        float brake = 0f;

        if (!hasThrottle)
            brake = coastBrakeToque;
        else if (pressingAgainstMotion)
            brake = Mathf.Abs(throttleInput) * maxBrakeTorque;
        else if (!isOverSpeed(forwardSpeed))
            motor = throttleInput * maxMotorTorque;

        if (handBrakeInput)
            motor = 0f;

        rearLeft.motorTorque = motor;
        rearRight.motorTorque = motor;

        float rearBrake = handBrakeInput ? handBrakeTorque : brake;
        frontLeft.brakeTorque = brake;
        frontRight.brakeTorque = brake;
        rearLeft.brakeTorque = rearBrake;
        rearRight.brakeTorque = rearBrake;
    }

    private bool isOverSpeed(float forwardSpeed) =>
        throttleInput > 0f ? forwardSpeed > maxForwardSpeed : -forwardSpeed > maxForwardSpeed;

    private static void SyncWheelMesh(WheelCollider wheel, Transform mesh)
    {
        wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
        mesh.SetPositionAndRotation(position, rotation);
    }
}
