using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;


/*  * ゴール処理クラス
 *  * ゴール処理を記載するスクリプトの基底クラス
 */

public abstract class BaseGoalAction : MonoBehaviour
{
    // ===========================================
    // ゴール処理
    // ===========================================
    public abstract UniTask Execute(CancellationToken token);
}
