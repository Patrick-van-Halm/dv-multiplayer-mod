using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkPlayerSync : MonoBehaviour
{
    public TrainCar Train { get; set; }
    public bool IsLocal { get; set; } = false;
    public string Username { get; set; }
    internal ushort Id;
    internal Vector3 currentWorldMove;
    internal Vector3 absPosition;
    private Vector3 prevPosition;
    private Vector3 newPosition;
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
            SingletonBehaviour<NetworkPlayerManager>.Instance.UpdateLocalPositionAndRotation(transform.position - WorldMover.currentMove, transform.rotation);
            prevPosition = transform.position;
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

    public void UpdateLocation(Vector3 pos, Quaternion? rot = null)
    {
        newPosition = pos;
        if(rot.HasValue)
            transform.rotation = rot.Value;
    }
}
