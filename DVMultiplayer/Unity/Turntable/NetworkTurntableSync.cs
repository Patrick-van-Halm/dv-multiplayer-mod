using DV.CabControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class NetworkTurntableSync : MonoBehaviour
{
    private TurntableController turntable;
    private LeverBase lever;
    private float prevTargetRotation;
    private void Awake()
    {
        turntable = GetComponent<TurntableController>();
        lever = turntable.leverGO.GetComponent<LeverBase>();
        SingletonBehaviour<CoroutineManager>.Instance.Run(CheckInput());
    }

    private IEnumerator CheckInput()
    {
        yield return new WaitUntil(() => lever.IsGrabbedOrHoverScrolled());
        yield return new WaitUntil(() => !lever.IsGrabbedOrHoverScrolled());
        OnTurntableRotationChanged(turntable.turntable.targetYRotation);
        yield return CheckInput();
    }

    private void OnTurntableRotationChanged(float targetYRotation)
    {
        if (SingletonBehaviour<NetworkTurntableManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableRotationChanged(turntable, targetYRotation);
    }
}