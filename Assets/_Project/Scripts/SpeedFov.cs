using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineCamera))]

public class SpeedFov : MonoBehaviour
{
    [SerializeField] private Rigidbody target;
    [SerializeField] private float baseFov = 45f;
    [SerializeField] private float maxFov = 60f;
    [SerializeField] private float speedForMaxFov = 20f;
    [SerializeField] private float smoothing = 4f;

    private CinemachineCamera cinemachineCamera;

    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    private void Update()
    {
        float speedFactor = Mathf.InverseLerp(0f, speedForMaxFov, target.linearVelocity.magnitude);
        float targetFov = Mathf.Lerp(baseFov, maxFov, speedFactor);
        float blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);

        cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFov, blend);
    }
}