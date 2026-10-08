using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ScaleCheckWalker : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.4f;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float jumpHeight = 2.1f;
    [SerializeField] private float gravity = -9.81f;


    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float lookSensitivity = 0.2f;
    [SerializeField] private float maxPitch = 85f;

    private CharacterController controller;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private float verticalVelocity;
    private float pitch;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");

    }

    private void OnEnable() => Cursor.lockState = CursorLockMode.Locked;
    private void OnDisable() => Cursor.lockState = CursorLockMode.None;

    private void Update()
    {
        Look();
        Move();
    }

    private void Look()
    {
        Vector2 delta = lookAction.ReadValue<Vector2>() * lookSensitivity;

        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        Vector3 horizontal = (transform.right * input.x + transform.forward * input.y) * speed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (controller.isGrounded && jumpAction.WasPressedThisFrame())
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
}