using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class RotateTowardsPlayer : MonoBehaviour
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