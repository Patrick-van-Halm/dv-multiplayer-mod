using UnityEngine;

internal class RotateTowardsPlayer : MonoBehaviour
{
    private Camera camera;

    private void Awake()
    {
        camera = PlayerManager.PlayerCamera;
    }

    private void Update()
    {
        transform.LookAt(transform.position + camera.transform.rotation * Vector3.forward, camera.transform.rotation * Vector3.up);
    }
}