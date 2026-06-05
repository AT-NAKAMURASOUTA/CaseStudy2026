using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MoveFloor : MonoBehaviour
{
    [Header("移動するオブジェクトのタグ")]
    [SerializeField]
    private List<string> targetTags = new List<string>();

    public List<string> tTags => targetTags;

    [Header("移動先の位置（複数の場合順番に移動する）")]
    [SerializeField]
    private List<Vector3> movePosition = new List<Vector3>();

    private int targetListIndex = 0;
    private int listMaxIndex = 0;
    private float totalLength = 0;
    private int currentListIndex = 0;

    /// <summary>
    /// Fixed
    /// 時間や距離に関わらず固定値
    /// ChangesDependingOnDistance
    /// 時間や距離に応じて変化
    /// </summary>
    enum MoveType
    {
        Fixed,
        ChangesDependingOnDistance
    }

    [Header("移動速度の基準")]
    [SerializeField]
    private MoveType moveType = MoveType.Fixed;

    [Header("固定の場合の移動速度")]
    [SerializeField]
    private float moveSpeed = 6.0f;

    [Header("何秒かけて移動するか")]
    [SerializeField]
    private float moveTime = 1.0f;
    private float sectionTime = 0.0f;
    private float moveCounter = 0.0f;

    //区間ごとに掛ける移動時間
    private List<float> moves = new List<float>();

    [Header("端に到達時の待機時間")]
    [SerializeField]
    private float waitTime = 1.0f;

    private float waitCounter = 0.0f;

    private bool isForward = false;
    private bool isMove = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isForward = true;
        isMove = false;

        movePosition.Insert(0, transform.position);

        listMaxIndex = movePosition.Count;
        currentListIndex = 0;

        for (int i = 0; i < listMaxIndex - 1; i++)
        {
            totalLength += (movePosition[i + 1] - movePosition[i]).magnitude;
        }

        for (int i = 0; i < listMaxIndex - 1; i++)
        {
            var sectionLength = (movePosition[i + 1] - movePosition[i]).magnitude;
            moves.Add(sectionLength / totalLength * moveTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        waitCounter += Time.deltaTime;
        if (waitCounter > waitTime)
        {
            bool isStop = false;
            if (moveType == MoveType.Fixed)
            {
                isStop = MoveFloor_Fixed();
            }
            else
            {
                isStop = MoveFloor_SpeedChanging();
            }
            if (isStop)
            {
                waitCounter = 0.0f;
            }
        }
    }

    private bool MoveFloor_Fixed()
    {
        if (moves.Count == 0)
        {
            return true;
        }

        bool isStop = false;

        Vector3 vecAC = transform.position - movePosition[currentListIndex];
        Vector3 vecBC = movePosition[currentListIndex + 1] - transform.position;
        Vector3 vecAB = movePosition[currentListIndex + 1] - movePosition[currentListIndex];

        float lenAC = vecAC.magnitude;
        float lenBC = vecBC.magnitude;
        float lenAB = vecAB.magnitude;

        Vector3 moveVec = vecAB.normalized * (isForward ? 1 : -1) * moveSpeed;
        transform.Translate(moveVec * Time.deltaTime);

        //Debug.Log("AC+AB : AB " + (lenAC + lenBC) + " : " + lenAB);

        if (lenAC + lenBC > lenAB * 1.01)
        {
            transform.position = (lenAC < lenBC ? movePosition[currentListIndex] : movePosition[currentListIndex + 1]);
            currentListIndex = isForward ? currentListIndex + 1 : currentListIndex - 1;
            if (currentListIndex < 0 || currentListIndex >= listMaxIndex - 1)
            {
                isForward = !isForward;
                isStop = true;
                currentListIndex = isForward ? currentListIndex + 1 : currentListIndex - 1;
            }
        }

        return isStop;
    }

    private bool MoveFloor_SpeedChanging()
    {
        if (moves.Count == 0)
        {
            return true;
        }

        bool isStop = false;
        moveCounter += Time.deltaTime;
        if (moveCounter / moves[currentListIndex - (isForward ? 0 : 1)] > 1.0f)
        {
            currentListIndex = isForward ? currentListIndex + 1 : currentListIndex - 1;
            if (currentListIndex <= 0 || currentListIndex >= listMaxIndex - 1)
            {
                isForward = !isForward;
                isStop = true;
            }
            moveCounter = 0.0f;
        }

        if (isForward)
        {
            var newPos = Vector3.Lerp(movePosition[currentListIndex], movePosition[currentListIndex + 1], moveCounter / moves[currentListIndex]);
            transform.position = newPos;
        }
        else
        {
            var newPos = Vector3.Lerp(movePosition[currentListIndex], movePosition[currentListIndex - 1], moveCounter / moves[currentListIndex - 1]);
            transform.position = newPos;
        }

        return isStop;
    }

    //指定したTagのオブジェクトをくっついて移動するようにする
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (targetTags.Contains(collision.gameObject.tag))
        {
            collision.transform.SetParent(transform);

            if (collision.transform.GetComponent<ParentOnParent>() == null)
                collision.transform.AddComponent<ParentOnParent>();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (targetTags.Contains(collision.gameObject.tag))
        {
            if (!collision.gameObject.activeInHierarchy) return;

            collision.transform.SetParent(null);

            if (collision.transform.TryGetComponent<ParentOnParent>(out var pop))
            {
                Destroy(pop);
            }
        }
    }

    //つぶされるなどして、プレイヤーがめり込むとミス判定を出す
    // Collisionの内側に小さめのTriggerを設置

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
        {
            return;
        }
        if (collision.TryGetComponent<PlayerRespawn>(out var playerRespawn))
        {
            playerRespawn.TriggerMiss();
        }
    }

    //-------------------------------------------

    private void OnDrawGizmos()
    {
        if (movePosition.Count > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, movePosition[0]);
            for (int i = 0; i < movePosition.Count - 1; i++)
            {
                Gizmos.DrawLine(movePosition[i], movePosition[i + 1]);
            }
        }
    }
}

public class ParentOnParent : MonoBehaviour
{
    private List<string> targetTags = new List<string>();
    private Transform parentTf;
    private void Awake()
    {
        if (transform.parent != null)
        {
            var mf = transform.parent.GetComponentInParent<MoveFloor>();
            parentTf = transform.parent.transform;
            targetTags = new List<string>(mf.tTags);
        }
    }

    //指定したTagのオブジェクトをくっついて移動するようにする
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (targetTags.Contains(collision.gameObject.tag))
        {
            if (collision.transform.GetComponent<ParentOnParent>() == null)
            {
                collision.transform.SetParent(parentTf);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (targetTags.Contains(collision.gameObject.tag))
        {
            if (!collision.gameObject.activeInHierarchy) return;

            if (collision.transform.GetComponent<ParentOnParent>() == null)
            {
                if (!collision.gameObject.activeInHierarchy) return;
                collision.transform.SetParent(null);
            }
        }
    }
}
