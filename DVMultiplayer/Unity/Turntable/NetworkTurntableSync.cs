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
    public float yRot;
    private void Awake()
    {
        turntable = GetComponent<TurntableController>();
        lever = turntable.leverGO.GetComponent<LeverBase>();
        yRot = turntable.turntable.targetYRotation;
        SingletonBehaviour<CoroutineManager>.Instance.Run(CheckInput());
        SingletonBehaviour<CoroutineManager>.Instance.Run(CheckPush());
    }

    private IEnumerator CheckInput()
    {
        yield return new WaitUntil(() => lever.IsGrabbedOrHoverScrolled());
        yield return new WaitUntil(() => {
            OnTurntableLeverAngleChanged(lever.Value);
            yRot = turntable.turntable.targetYRotation;
            return !lever.IsGrabbedOrHoverScrolled() && lever.Value > .45f && lever.Value < .55f;
        });
        yRot = turntable.turntable.targetYRotation;
        OnTurntableRotationChanged(turntable.turntable.targetYRotation);
        yield return CheckInput();
    }

    private IEnumerator CheckPush()
    {
        yield return new WaitUntil(() => !lever.IsGrabbedOrHoverScrolled() && turntable.turntable.targetYRotation != yRot);
        yRot = turntable.turntable.targetYRotation;
        OnTurntableRotationChanged(turntable.turntable.targetYRotation);
        yield return CheckPush();
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