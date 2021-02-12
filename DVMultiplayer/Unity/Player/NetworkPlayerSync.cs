using UnityEngine;

internal class NetworkPlayerSync : MonoBehaviour
{
    public TrainCar Train { get; set; }
    public bool IsLocal { get; set; } = false;
    public string Username { get; set; }
    public string[] Mods { get; set; }
    internal ushort Id;
    private Vector3 prevPosition;
    private Vector3 newPosition;
    private Vector3 absPosition;
    internal bool IsLoaded;
    private const float SYNC_THRESHOLD = 0.1f;

#pragma warning disable IDE0051 // Remove unused private members
    private void Start()
    {
        absPosition = transform.position;
        newPosition = absPosition;
    }

    private void Update()
    {
        if (!IsLocal)
        {
            transform.position = Vector3.Lerp(transform.position, newPosition + WorldMover.currentMove, 15 * (Time.deltaTime / 2));
            return;
        }

        if (prevPosition == null || Vector3.Distance(prevPosition, transform.position) > SYNC_THRESHOLD)
        {
            // Main.DebugLog("Player location changed sending new location");
            SingletonBehaviour<NetworkPlayerManager>.Instance.UpdateLocalPositionAndRotation(transform.position, transform.rotation);
            prevPosition = transform.position;
        }
    }
#pragma warning restore IDE0051 // Remove unused private members

    public void UpdateLocation(Vector3 pos, Quaternion? rot = null)
    {
        newPosition = pos;
        if (rot.HasValue)
            transform.rotation = rot.Value;
    }
}
