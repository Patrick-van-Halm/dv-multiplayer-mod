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
    private float prevLeverAngle;
    private void Awake()
    {
        turntable = GetComponent<TurntableController>();
        lever = turntable.leverGO.GetComponent<LeverBase>();
        SingletonBehaviour<CoroutineManager>.Instance.Run(CheckInput());
    }

    private IEnumerator CheckInput()
    {
        yield return new WaitUntil(() => lever.IsGrabbedOrHoverScrolled());
        yield return new WaitUntil(() => {
            OnTurntableLeverAngleChanged(lever.Value);
            return !lever.IsGrabbedOrHoverScrolled();
        });
        OnTurntableRotationChanged(turntable.turntable.targetYRotation);
        yield return CheckInput();
    }

    private void OnTurntableLeverAngleChanged(float value)
    {
        if (prevLeverAngle != value)
        {
            prevLeverAngle = value;
            SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableRotationChanged(turntable, value, true);
        }
    }

    private void OnTurntableRotationChanged(float targetYRotation)
    {
        SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableRotationChanged(turntable, targetYRotation, false);
    }
}