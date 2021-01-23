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
        junction = GetComponent<Junction>();
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