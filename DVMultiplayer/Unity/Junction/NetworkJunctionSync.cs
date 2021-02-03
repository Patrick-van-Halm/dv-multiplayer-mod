using DVMultiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkJunctionSync : MonoBehaviour
{
    Junction junction;
    private void Awake()
    {
        Main.DebugLog($"NetworkJunctionSync initalized");
        junction = GetComponent<Junction>();
        Main.DebugLog($"NetworkJunctionSync Listening to Junction change event");
        junction.Switched += OnJunctionSwitched;
    }

    private void OnJunctionSwitched(Junction.SwitchMode mode, int branchNum)
    {
        if (SingletonBehaviour<NetworkJunctionManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkJunctionManager>.Instance.OnJunctionSwitched(junction.position, mode, branchNum == 0);
    }

    private void OnDestroy()
    {
        junction.Switched -= OnJunctionSwitched;
    }
}