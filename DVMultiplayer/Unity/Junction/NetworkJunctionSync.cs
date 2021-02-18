using DVMultiplayer;
using UnityEngine;

internal class NetworkJunctionSync : MonoBehaviour
{
    private Junction junction;
    private void Awake()
    {
        Main.Log($"NetworkJunctionSync initalized");
        junction = GetComponent<Junction>();
        Main.Log($"NetworkJunctionSync Listening to Junction change event");
        junction.Switched += OnJunctionSwitched;
    }

    private void OnJunctionSwitched(Junction.SwitchMode mode, int branchNum)
    {
        SingletonBehaviour<NetworkJunctionManager>.Instance.OnJunctionSwitched(junction.position, mode, branchNum == 0);
    }

    private void OnDestroy()
    {
        junction.Switched -= OnJunctionSwitched;
    }
}