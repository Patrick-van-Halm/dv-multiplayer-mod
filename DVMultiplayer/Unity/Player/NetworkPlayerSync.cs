using UnityEngine;
using UnityEngine.UI;

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
    private const float SYNC_THRESHOLD = 0.05f;
    private int ping = 0;

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
            float step = 10 * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, newPosition + WorldMover.currentMove, );
            transform.GetChild(0).Find("Ping").GetComponent<Text>().text = $"{ping}ms";
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

    public void UpdateLocation(Vector3 pos, int ping, Quaternion? rot = null)
    {
        newPosition = pos;
        this.ping = ping;
        if (rot.HasValue)
            transform.rotation = rot.Value;
    }
}
