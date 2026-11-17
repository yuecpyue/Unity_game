using UnityEngine;

/// <summary>
/// 投射物腳本 (水手裡劍 Water Shuriken)
/// 掛載於水手裡劍 Prefab 上，生成後直線飛向目標敵人，命中時造成傷害並銷毀自身
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("=== 投射物飛行設定 ===")]
    [Tooltip("飛行速度")]
    [SerializeField] private float speed = 16f;

    [Tooltip("命中判定距離 (到達此距離視為命中)")]
    [SerializeField] private float hitDistance = 0.6f;

    [Tooltip("最長存在時間 (秒)，避免沒打中卡在場景中")]
    [SerializeField] private float maxLifeTime = 5f;

    [Tooltip("自轉速度 (度/秒)，營造手裡劍高速旋轉感")]
    [SerializeField] private float rotationSpeed = 1080f;

    [Tooltip("旋轉軸向 (通常為 Z 或 Y 軸，預設沿著自身前進軸旋轉)")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    private BattleUnit targetUnit;
    private int damage = 25;
    private System.Action onHitCallback;
    private bool hasHit = false;

    /// <summary>
    /// 初始化投射物目標、傷害與命中回調
    /// </summary>
    public void Initialize(BattleUnit target, int dmg, System.Action onHit = null)
    {
        this.targetUnit = target;
        this.damage = dmg;
        this.onHitCallback = onHit;
        Destroy(gameObject, maxLifeTime);
    }

    private void Update()
    {
        if (hasHit) return;

        // 手裡劍自轉效果
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);

        // 若目標已不存在，銷毀投射物
        if (targetUnit == null)
        {
            Destroy(gameObject);
            return;
        }

        // 計算目標受擊中心點 (稍微往上提高 0.8 公尺至胸腹部)
        Vector3 targetCenter = targetUnit.transform.position + Vector3.up * 0.8f;

        // 朝向目標直線平移
        transform.position = Vector3.MoveTowards(transform.position, targetCenter, speed * Time.deltaTime);

        // 檢查是否抵達命中距離
        if (Vector3.Distance(transform.position, targetCenter) <= hitDistance)
        {
            ApplyHit();
        }
    }

    /// <summary>
    /// 支援物理 Trigger 碰撞觸發
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (targetUnit != null)
        {
            // 若撞擊到的物件是目標或其子物件
            if (other.transform == targetUnit.transform || other.transform.IsChildOf(targetUnit.transform))
            {
                ApplyHit();
            }
        }
    }

    /// <summary>
    /// 執行命中結算與銷毀
    /// </summary>
    private void ApplyHit()
    {
        if (hasHit) return;
        hasHit = true;

        if (targetUnit != null)
        {
            targetUnit.TakeDamage(damage);
        }

        // 執行外部回調 (例如通知 BattleSystem 更新 UI 或結算)
        onHitCallback?.Invoke();

        // 銷毀投射物
        Destroy(gameObject);
    }
}
