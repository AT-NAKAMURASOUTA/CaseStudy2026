using UnityEngine;

[ExecuteAlways]
public sealed class HangLeverPivotHandle : MonoBehaviour
{
    [SerializeField]
    private float gizmoRadius = 0.08f;

    [SerializeField]
    private float gizmoCrossSize = 0.12f;

    private void OnDrawGizmos()
    {
        DrawPivotGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        DrawPivotGizmo();
    }

    private void DrawPivotGizmo()
    {
        // Pivotの位置が見やすいようにギズモ表示
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        // 横線
        Gizmos.DrawLine(
            transform.position + Vector3.left * gizmoCrossSize,
            transform.position + Vector3.right * gizmoCrossSize
        );

        // 縦線
        Gizmos.DrawLine(
            transform.position + Vector3.up * gizmoCrossSize,
            transform.position + Vector3.down * gizmoCrossSize
        );
    }
}
