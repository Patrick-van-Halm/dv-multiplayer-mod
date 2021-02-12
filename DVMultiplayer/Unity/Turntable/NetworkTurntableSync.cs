using DV.CabControls;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class NetworkTurntableSync : MonoBehaviour
{
    private TurntableController turntable;
    private LeverBase lever;
    private float prevLeverAngle;
    private Transform playerCameraTransform;
    private TurntableControlKeyboardInput keyboardInput;
    private Coroutine coroutineInputLever;
    private readonly List<TrainCar> trainsOnTurntable = new List<TrainCar>();
    private void Awake()
    {
        turntable = GetComponent<TurntableController>();
        lever = turntable.leverGO.GetComponent<LeverBase>();
        playerCameraTransform = PlayerManager.PlayerCamera.transform;
        coroutineInputLever = SingletonBehaviour<CoroutineManager>.Instance.Run(CheckInputLever());
        turntable.Snapped += Turntable_Snapped;
        SingletonBehaviour<CoroutineManager>.Instance.Run(DisableKeyboardInput());
    }

    private IEnumerator DisableKeyboardInput()
    {
        yield return new WaitUntil(() =>
        {
            keyboardInput = turntable.GetComponentInChildren<TurntableControlKeyboardInput>();
            return keyboardInput;
        });
        keyboardInput.enabled = false;
    }

    private void OnDestroy()
    {
        SingletonBehaviour<CoroutineManager>.Instance.Stop(coroutineInputLever);
        turntable.Snapped -= Turntable_Snapped;
        if (keyboardInput)
            keyboardInput.enabled = true;
    }

    private void Turntable_Snapped()
    {
        if (!SingletonBehaviour<NetworkTurntableManager>.Instance.IsChangeByNetwork)
            SingletonBehaviour<NetworkTurntableManager>.Instance.OnTurntableSnap(turntable, turntable.turntable.currentYRotation);
    }

    private IEnumerator CheckInputLever()
    {
        yield return new WaitUntil(() =>
        {
            return (lever.Value < .45f || lever.Value > .55f) && lever.IsGrabbedOrHoverScrolled();
        });
        yield return new WaitUntil(() =>
        {
            OnTurntableLeverAngleChanged(lever.Value);
            return lever.Value > .45f && lever.Value < .55f && !lever.IsGrabbedOrHoverScrolled();
        });
        OnTurntableRotationChanged(turntable.turntable.targetYRotation);
        yield return CheckInputLever();
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