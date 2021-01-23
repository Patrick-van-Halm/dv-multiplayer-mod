using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkPlayerSync : MonoBehaviour
{
    public TrainCar train { get; set; }
    public bool IsLocal { get; set; } = false;
    private Vector3 prevPosition;
    private Vector3 newPosition;
    internal ushort Id;
    private const float SYNC_THRESHOLD = 0.1f;

    private void Update()
    {
        if (!IsLocal)
        {
            if (newPosition == null)
                newPosition = transform.position;
            transform.position = Vector3.Lerp(transform.position, newPosition, 15 * (Time.deltaTime / 2));
            return;
        }

        if(prevPosition == null || Vector3.Distance(prevPosition, transform.position) > SYNC_THRESHOLD)
        {
            prevPosition = transform.position;
            SingletonBehaviour<NetworkPlayerManager>.Instance.UpdateLocalPositionAndRotation(new Vector3(transform.position.x, transform.position.y + 1, transform.position.z), transform.rotation);
        }
    }

    //private void FixedUpdate()
    //{
    //    if (IsLocal)
    //        return;
    //    if(olderPosition != null && prevPosition != null)
    //    {
    //        UpdateLocation(transform.position + trainCar.rb.velocity / 2 * Time.deltaTime, transform.rotation);
    //    }
    //}

    public void UpdateLocation(Vector3 pos, Quaternion rot)
    {
        newPosition = pos;
        transform.rotation = rot;
    }
}
