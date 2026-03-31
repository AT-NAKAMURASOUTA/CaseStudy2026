using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpecialArea")]
public class ScriptableObject_SpecialAreaData : ScriptableObject
{
    //加速度両
    public float accelerationMagnification = 1.3f;
    //低重力両
    public float lowGravityMagnification = 1.3f;

    //加速度量の取得
    public float GetAccelerationMagnification()
    {
        return accelerationMagnification;
    }

    //低重力量の取得
    public float GetLowGravityMagnification()
    {
        return lowGravityMagnification;

    }
}
