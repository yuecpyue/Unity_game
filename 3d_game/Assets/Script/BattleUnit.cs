using System.Collections;
using UnityEngine;

/// <summary>
/// 戰鬥單位組件 (BattleUnit)
/// 涵蓋階段一基礎數值、階段二近戰衝刺位移/受擊閃白/動畫觸發、階段三遠距離攻擊發射
/// </summary>
public class BattleUnit : MonoBehaviour
{
    // ==========================================
    // [階段一] 基礎數值設定 (Phase 1)
    // ==========================================
    [Header("=== 角色基本數值 (Phase 1) ===")]
    [Tooltip("角色名稱")]
    [SerializeField] private string unitName = "甲賀忍蛙";

    [Tooltip("最大血量")]
    [SerializeField] private int maxHP = 100;

    [Tooltip("當前血量 (執行時於 Inspector 觀察)")]
    [SerializeField] private int currentHP;

    [Tooltip("基礎攻擊力")]
    [SerializeField] private int damage = 25;

    // ==========================================
    // [階段二] 動畫與視覺反饋設定 (Phase 2)
    // ==========================================
    [Header("=== 動畫與視覺設定 (Phase 2) ===")]
    [Tooltip("角色 Animator 元件 (若未指定會在 Start 自動尋找)")]
    [SerializeField] private Animator animator;

    [Tooltip("受擊閃白 Renderer (支援 SkinnedMeshRenderer 或 MeshRenderer，若未指定自動搜尋)")]
    [SerializeField] private Renderer unitRenderer;

    [Tooltip("近戰衝刺移動速度")]
    [SerializeField] private float moveSpeed = 8f;

    // ==========================================
    // [階段三] 遠距離投射物設定 (Phase 3)
    // ==========================================
    [Header("=== 遠距離技能設定 (Phase 3) ===")]
    [Tooltip("投射物發射點 (可為手部節點，若留空預設為角色前方)")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Tooltip("水手裡劍投射物 Prefab")]
    [SerializeField] private GameObject projectilePrefab;

    // 內部座標與材質快取
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Color originalColor = Color.white;
    private string colorPropName = "_BaseColor";
    private Material instanceMaterial;
    private Coroutine flashCoroutine;

    #region 公開屬性 (Getters)
    public string UnitName => unitName;
    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int Damage => damage;
    public bool IsDead => currentHP <= 0;
    public Vector3 StartPosition => startPosition;
    #endregion

    private void Awake()
    {
        // 記錄初始座標與旋轉，用於近戰衝刺攻擊後退回原位
        startPosition = transform.position;
        startRotation = transform.rotation;

        // 初始化血量為最大值
        currentHP = maxHP;

        // 自動取得 Animator
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // 重要限制防護：關閉 Apply Root Motion，角色移動完全由 C# 控制
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // 自動抓取 Renderer (優先尋找 SkinnedMeshRenderer，其次 MeshRenderer)
        if (unitRenderer == null)
        {
            unitRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (unitRenderer == null)
            {
                unitRenderer = GetComponentInChildren<MeshRenderer>();
            }
        }

        // 快取受擊閃白所需材質與顏色屬性
        InitMaterialColor();
    }

    private void Start()
    {
        // 確保 startPosition 與 currentHP 正確
        startPosition = transform.position;
        startRotation = transform.rotation;
        if (currentHP <= 0) currentHP = maxHP;
    }

    /// <summary>
    /// 初始化材質顏色 (支援 URP 的 _BaseColor 與標準 Shader 的 _Color)
    /// </summary>
    private void InitMaterialColor()
    {
        if (unitRenderer != null)
        {
            // 存取 .material 會建立該 GameObject 的實例，不會影響 Project 內的原始資產
            instanceMaterial = unitRenderer.material;
            if (instanceMaterial != null)
            {
                if (instanceMaterial.HasProperty("_BaseColor"))
                {
                    colorPropName = "_BaseColor";
                    originalColor = instanceMaterial.GetColor("_BaseColor");
                }
                else if (instanceMaterial.HasProperty("_Color"))
                {
                    colorPropName = "_Color";
                    originalColor = instanceMaterial.GetColor("_Color");
                }
            }
        }
    }

    // ==========================================
    // [階段一 & 二] 受到傷害方法
    // ==========================================
    /// <summary>
    /// 受到傷害 (TakeDamage)
    /// 扣減血量、觸發 Hit 動畫與受擊閃白
    /// </summary>
    /// <param name="dmg">受到的傷害值</param>
    /// <returns>角色是否陣亡 (true 為陣亡)</returns>
    public bool TakeDamage(int dmg)
    {
        currentHP = Mathf.Clamp(currentHP - dmg, 0, maxHP);
        Debug.Log($"<color=orange>[{unitName}]</color> 受到 <color=yellow>{dmg}</color> 點傷害！剩餘血量: <color=cyan>{currentHP}/{maxHP}</color>");

        // [階段二] 觸發 Hit 受擊動畫
        TriggerHitAnimation();

        // [階段二] 觸發受擊閃白
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(HitFlashCoroutine());

        return IsDead;
    }

    /// <summary>
    /// 觸發 Hit 動畫
    /// </summary>
    private void TriggerHitAnimation()
    {
        if (animator == null) return;

        // 安全防護：檢查 Animator 是否存在 "Hit" 參數，避免報出缺少參數警告
        if (HasParameter(animator, "Hit"))
        {
            animator.SetTrigger("Hit");
        }
    }

    /// <summary>
    /// 觸發 Attack 動畫
    /// </summary>
    private void TriggerAttackAnimation()
    {
        if (animator == null) return;

        if (HasParameter(animator, "Attack"))
        {
            animator.SetTrigger("Attack");
        }
    }

    // ==========================================
    // [階段二] 受擊閃白協程 (Hit Flash)
    // ==========================================
    /// <summary>
    /// 在 0.15 秒內將材質顏色變為純白，隨後恢復原色
    /// </summary>
    private IEnumerator HitFlashCoroutine()
    {
        if (instanceMaterial == null) yield break;

        // 設置為純白
        instanceMaterial.SetColor(colorPropName, Color.white);

        // 持續 0.15 秒
        yield return new WaitForSeconds(0.15f);

        // 恢復原始顏色
        if (instanceMaterial != null)
        {
            instanceMaterial.SetColor(colorPropName, originalColor);
        }
        flashCoroutine = null;
    }

    // ==========================================
    // [階段二] 近戰衝刺與退回 (Scripted Movement)
    // ==========================================
    /// <summary>
    /// 近戰攻擊協程：
    /// 平順移動到敵人前方 1.5 公尺處 -> 觸發 Attack 動畫 -> 等待 0.5 秒敵人扣血 -> 平順退回 startPosition
    /// </summary>
    public IEnumerator MeleeAttackCoroutine(BattleUnit target, System.Action onHitCallback)
    {
        if (target == null) yield break;

        // 確保初始座標記錄正確
        if (startPosition == Vector3.zero)
        {
            startPosition = transform.position;
        }

        Vector3 targetPos = target.transform.position;

        // 計算敵人前方 1.5 公尺處的停頓點
        Vector3 directionFromTarget = (transform.position - targetPos).normalized;
        if (directionFromTarget.sqrMagnitude < 0.001f)
        {
            directionFromTarget = -target.transform.forward;
        }
        Vector3 attackStopPos = targetPos + directionFromTarget * 1.5f;
        attackStopPos.y = transform.position.y; // 保持同高度地面

        // 面向敵人
        LookAtTarget(targetPos);

        // 1. 平順衝刺移動至敵人前方 1.5 公尺處
        while (Vector3.Distance(transform.position, attackStopPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, attackStopPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = attackStopPos;

        // 2. 觸發 Attack 動畫
        TriggerAttackAnimation();

        // 3. 等待 0.5 秒後結算敵人扣血
        yield return new WaitForSeconds(0.5f);
        onHitCallback?.Invoke();

        // 稍微停留感受打擊感
        yield return new WaitForSeconds(0.2f);

        // 4. 平順退回 startPosition
        while (Vector3.Distance(transform.position, startPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = startPosition;

        // 恢復面向方向
        LookAtTarget(targetPos);
    }

    // ==========================================
    // [階段三] 遠距離攻擊 (水手裡劍投射物)
    // ==========================================
    /// <summary>
    /// 遠距離攻擊協程：
    /// 原地觸發 Attack 動畫 -> 生成投射物水手裡劍 -> 直線飛行命中 -> 結算傷害
    /// </summary>
    public IEnumerator RangedAttackCoroutine(BattleUnit target, GameObject overrideProjectilePrefab, System.Action onHitCallback)
    {
        if (target == null) yield break;

        Vector3 targetPos = target.transform.position;
        LookAtTarget(targetPos);

        // 1. 原地觸發 Attack 動畫
        TriggerAttackAnimation();

        // 等待揮手前搖動畫
        yield return new WaitForSeconds(0.25f);

        // 2. 計算投射物生成位置
        Vector3 spawnPos = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position + Vector3.up * 1.0f + transform.forward * 0.7f;

        GameObject prefabToUse = overrideProjectilePrefab != null ? overrideProjectilePrefab : projectilePrefab;

        bool hitHandled = false;

        if (prefabToUse != null)
        {
            GameObject projGo = Instantiate(prefabToUse, spawnPos, Quaternion.LookRotation(transform.forward));
            Projectile proj = projGo.GetComponent<Projectile>();
            if (proj == null)
            {
                proj = projGo.AddComponent<Projectile>();
            }

            // 若場景中存在即時氣象連動管理器，自動套用水屬性技能倍率
            int finalDmg = damage;
            if (WeatherBattleManager.Instance != null)
            {
                finalDmg = Mathf.RoundToInt(damage * WeatherBattleManager.Instance.WaterDamageMultiplier);
            }

            // 初始化水手裡劍目標與回調
            proj.Initialize(target, finalDmg, () =>
            {
                onHitCallback?.Invoke();
                hitHandled = true;
            });

            // 等待手裡劍抵達目標或超時
            float timeout = 3.5f;
            while (!hitHandled && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // 防護機制：若尚未指定投射物 Prefab，延遲 0.3 秒直接觸發命中
            Debug.LogWarning($"[{unitName}] 未指定水手裡劍 Prefab，使用預設延遲命中！");
            yield return new WaitForSeconds(0.35f);
            onHitCallback?.Invoke();
        }

        // 後搖收招等待
        yield return new WaitForSeconds(0.3f);
    }

    private void LookAtTarget(Vector3 targetPos)
    {
        Vector3 lookDir = targetPos - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    /// <summary>
    /// 檢查 Animator 是否包含特定參數
    /// </summary>
    private static bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }
}
