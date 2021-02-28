using DVMultiplayer;
using UnityEngine;

internal class NetworkJunctionSync : MonoBehaviour
{
    private Junction junction;

    public uint Id { get; internal set; }

    private void Awake()
    {
        Main.Log($"NetworkJunctionSync initalized");
        junction = GetComponent<Junction>();
        Main.Log($"NetworkJunctionSync Listening to Junction change event");
        junction.Switched += OnJunctionSwitched;
    }

    private void OnJunctionSwitched(Junction.SwitchMode mode, int branchNum)
    {
        SingletonBehaviour<NetworkJunctionManager>.Instance.OnJunctionSwitched(Id, mode, branchNum == 0);
    }

    private void OnDestroy()
    {
        junction.Switched -= OnJunctionSwitched;
    }
}