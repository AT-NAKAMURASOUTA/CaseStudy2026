using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;


/*  * 基底アクション処理クラス
 */

public abstract class BaseAction : MonoBehaviour
{
    // ===========================================
    // アクション処理
    // ===========================================
    public abstract UniTask Execute(CancellationToken token);
}
