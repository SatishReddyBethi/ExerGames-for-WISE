using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventFunctions : MonoBehaviour
{

    public DeviceManager DM;
    public PlayerController PC;
    public void NextActivityIteration()
    {
        PC.ActivityIteration++;
    }

    public void CameraShiftBack()
    {
        DM.CameraViewValue = 0;
        DM.ChangeCamerawhileAnimation();
    }

    public void CameraShiftLeft()
    {
        DM.CameraViewValue = 3;
        DM.ChangeCamerawhileAnimation();
    }

    public void CameraShiftRight()
    {
        DM.CameraViewValue = 2;
        DM.ChangeCamerawhileAnimation();
    }

    public void CameraShiftFront()
    {
        DM.CameraViewValue = 1;
        DM.ChangeCamerawhileAnimation();
    }
}
