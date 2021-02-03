using DV.CabControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkTurntableSync : MonoBehaviour
{
    private TurntableController turntable;
    private float prevRotation;
    private void Awake()
    {
        turntable = GetComponent<TurntableController>();
    }

    private void Update()
    {
        if(turntable.turntable.targetYRotation != prevRotation)
        {

            prevRotation = turntable.turntable.currentYRotation;
            OnTurntableRotationChanged(turntable.turntable.currentYRotation);
        }
    }

    private void OnTurntableRotationChanged(float targetYRotation)
    {
        if (SingletonBehaviour<NetworkTurntableManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableRotationChanged(turntable, targetYRotation);
    }
}