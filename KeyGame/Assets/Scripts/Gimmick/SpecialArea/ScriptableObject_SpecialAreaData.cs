using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpecialArea")]
public class ScriptableObject_SpecialAreaData : ScriptableObject
{
    //加速度量
    public float accelerationMagnification = 1.3f;
    //低重力量
    public float lowGravityMagnification = 0.7f;


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
