using DVMultiplayer;
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
    public string[] Mods { get; set; }
    internal ushort Id;
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
           // Main.DebugLog("Player location changed sending new location");
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
