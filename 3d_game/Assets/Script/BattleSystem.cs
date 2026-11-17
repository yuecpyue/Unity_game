using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 戰鬥回合狀態列舉 (Phase 1 規範命名)
/// </summary>
public enum BattleState
{
    StartBattle,   // 戰鬥開始準備階段
    PlayerTurn,    // 玩家回合 (等待選擇行動)
    PlayerAction,  // 玩家執行攻擊行動中
    EnemyTurn,     // 敵人回合 (思考/準備行動)
    EnemyAction,   // 敵人執行攻擊行動中
    Win,           // 戰鬥獲勝 (敵人全倒)
    Lose           // 戰鬥失敗 (玩家陣亡)
}

/// <summary>
/// 回合制戰鬥核心狀態機系統 (BattleSystem)
/// 涵蓋：
/// [階段一] FSM 協程流程與狀態切換、Console Log 印出
/// [階段二] 戰鬥動作連動、位移與受擊反饋調度
/// [階段三] 雙招式按鈕綁定 (近戰/水手裡劍)、按鈕互動狀態鎖定、血條 Slider 同步更新
/// </summary>
public class BattleSystem : MonoBehaviour
{
    // ==========================================
    // [階段一] 戰鬥單位綁定 (Phase 1)
    // ==========================================
    [Header("=== 戰鬥單位設定 (Phase 1) ===")]
    [Tooltip("玩家戰鬥單位 (甲賀忍蛙)")]
    [SerializeField] private BattleUnit playerUnit;

    [Tooltip("敵人戰鬥單位 (測試 Cube 或敵方角色)")]
    [SerializeField] private BattleUnit enemyUnit;

    [Tooltip("若未綁定 UI 按鈕時，是否自動模擬玩家行動 (方便階段一無 UI 時快速跑通 Console 測試)")]
    [SerializeField] private bool autoPlayIfNoUI = true;

    // ==========================================
    // [階段三] 投射物與 UI 元件設定 (Phase 3)
    // ==========================================
    [Header("=== 遠距離投射物設定 (Phase 3) ===")]
    [Tooltip("水手裡劍 Prefab (可選，若 playerUnit 內部已指定可留空)")]
    [SerializeField] private GameObject waterShurikenPrefab;

    [Header("=== UI 元件綁定 (Phase 3) ===")]
    [Tooltip("近戰衝刺攻擊按鈕")]
    [SerializeField] private Button meleeAttackButton;

    [Tooltip("遠距離水手裡劍按鈕")]
    [SerializeField] private Button rangedAttackButton;

    [Tooltip("玩家血條 Slider")]
    [SerializeField] private Slider playerHPSlider;

    [Tooltip("敵人血條 Slider")]
    [SerializeField] private Slider enemyHPSlider;

    [Tooltip("玩家血量文字 (可選 TMP)")]
    [SerializeField] private TextMeshProUGUI playerHPText;

    [Tooltip("敵人血量文字 (可選 TMP)")]
    [SerializeField] private TextMeshProUGUI enemyHPText;

    [Tooltip("戰鬥狀態/提示文字 (可選 TMP)")]
    [SerializeField] private TextMeshProUGUI battleStatusText;

    // 當前戰鬥狀態
    private BattleState state;
    public BattleState CurrentState => state;

    private void Start()
    {
        state = BattleState.StartBattle;
        StartCoroutine(SetupBattleRoutine());
    }

    // ==========================================
    // [階段一] 戰鬥初始化流程 (StartBattle)
    // ==========================================
    private IEnumerator SetupBattleRoutine()
    {
        Debug.Log("<color=#00E5FF><b>[BattleSystem]</b> ===== 戰鬥開始！狀態進入: [StartBattle] =====</color>");

        // 鎖定所有 UI 按鈕，避免初始化期間誤觸
        SetButtonsInteractable(false);

        // 自動防護：若未指派戰鬥單位，嘗試在場景中自動尋找
        if (playerUnit == null || enemyUnit == null)
        {
            BattleUnit[] units = FindObjectsByType<BattleUnit>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (playerUnit == null && (u.gameObject.name.Contains("Player") || u.gameObject.name.Contains("forg") || u.gameObject.name.Contains("seven")))
                {
                    playerUnit = u;
                }
                else if (enemyUnit == null && u != playerUnit)
                {
                    enemyUnit = u;
                }
            }
        }

        if (playerUnit == null || enemyUnit == null)
        {
            Debug.LogError("[BattleSystem] 錯誤：場景中未設定 playerUnit 或 enemyUnit！請在 Inspector 指派。");
            yield break;
        }

        Debug.Log($"[BattleSystem] 玩家角色: [{playerUnit.UnitName}] (HP: {playerUnit.CurrentHP}/{playerUnit.MaxHP}) VS 敵方角色: [{enemyUnit.UnitName}] (HP: {enemyUnit.CurrentHP}/{enemyUnit.MaxHP})");

        if (battleStatusText != null)
        {
            battleStatusText.text = $"戰鬥即將開始！\n{playerUnit.UnitName} VS {enemyUnit.UnitName}";
        }

        // 初始化血條 UI
        UpdateHPUI();

        // 等待 1.5 秒戰鬥開始前置過場
        yield return new WaitForSeconds(1.5f);

        // 進入玩家回合
        EnterPlayerTurn();
    }

    // ==========================================
    // [階段一 & 三] 玩家回合流程 (PlayerTurn)
    // ==========================================
    private void EnterPlayerTurn()
    {
        state = BattleState.PlayerTurn;
        Debug.Log($"<color=#76FF03><b>[BattleSystem]</b> 輪到玩家回合！狀態進入: [PlayerTurn] (剩餘血量: {playerUnit.CurrentHP}/{playerUnit.MaxHP})</color>");

        if (battleStatusText != null)
        {
            battleStatusText.text = "【玩家回合】請選擇攻擊方式！";
        }

        // [階段三] 只有在 PlayerTurn 狀態下，UI 攻擊按鈕才可點擊
        SetButtonsInteractable(true);

        // 若尚未設定 UI 按鈕 (如階段一測試環境)，啟用自動出招以便在 Console 觀察完整狀態輪轉
        bool hasButtons = (meleeAttackButton != null || rangedAttackButton != null);
        if (!hasButtons && autoPlayIfNoUI)
        {
            Debug.Log("[BattleSystem] (提示: 偵測到未綁定 UI 按鈕，自動於 1.2 秒後發動近戰攻擊以供測試)");
            StartCoroutine(AutoPlayerActionRoutine());
        }
    }

    private IEnumerator AutoPlayerActionRoutine()
    {
        yield return new WaitForSeconds(1.2f);
        if (state == BattleState.PlayerTurn)
        {
            OnMeleeAttackButton();
        }
    }

    // ==========================================
    // [階段三] UI 攻擊按鈕點擊方法 (Public)
    // ==========================================
    /// <summary>
    /// 招式一：近戰衝刺攻擊按鈕事件
    /// </summary>
    public void OnMeleeAttackButton()
    {
        if (state != BattleState.PlayerTurn) return;

        // 行動期間立即鎖定按鈕，防止重複點擊
        SetButtonsInteractable(false);
        state = BattleState.PlayerAction;
        Debug.Log("<color=#FFD600><b>[BattleSystem]</b> 玩家選擇 [近戰衝刺攻擊]！狀態進入: [PlayerAction]</color>");

        if (battleStatusText != null)
        {
            battleStatusText.text = $"{playerUnit.UnitName} 向敵人衝刺突進！";
        }

        StartCoroutine(PlayerMeleeActionRoutine());
    }

    /// <summary>
    /// 招式二：水手裡劍遠距離攻擊按鈕事件
    /// </summary>
    public void OnRangedAttackButton()
    {
        if (state != BattleState.PlayerTurn) return;

        SetButtonsInteractable(false);
        state = BattleState.PlayerAction;
        Debug.Log("<color=#00B0FF><b>[BattleSystem]</b> 玩家選擇 [水手裡劍遠距攻擊]！狀態進入: [PlayerAction]</color>");

        if (battleStatusText != null)
        {
            battleStatusText.text = $"{playerUnit.UnitName} 凝聚水手裡劍並擲出！";
        }

        StartCoroutine(PlayerRangedActionRoutine());
    }

    // ==========================================
    // [階段二 & 三] 玩家行動協程實作 (PlayerAction)
    // ==========================================
    private IEnumerator PlayerMeleeActionRoutine()
    {
        // 執行近戰衝刺 -> 攻擊動畫 -> 傷害回調 -> 平順退回
        yield return StartCoroutine(playerUnit.MeleeAttackCoroutine(enemyUnit, () =>
        {
            bool isDead = enemyUnit.TakeDamage(playerUnit.Damage);
            UpdateHPUI();
            Debug.Log($"[BattleSystem] 攻擊命中！{enemyUnit.UnitName} 扣除 {playerUnit.Damage} 點血量，當前 HP: {enemyUnit.CurrentHP}/{enemyUnit.MaxHP}");
        }));

        yield return new WaitForSeconds(0.4f);

        // 判斷勝負或轉移回合
        CheckBattleAfterPlayerAction();
    }

    private IEnumerator PlayerRangedActionRoutine()
    {
        // 執行遠距離發射水手裡劍
        yield return StartCoroutine(playerUnit.RangedAttackCoroutine(enemyUnit, waterShurikenPrefab, () =>
        {
            UpdateHPUI();
            Debug.Log($"[BattleSystem] 水手裡劍精準命中！{enemyUnit.UnitName} 當前 HP: {enemyUnit.CurrentHP}/{enemyUnit.MaxHP}");
        }));

        yield return new WaitForSeconds(0.4f);

        CheckBattleAfterPlayerAction();
    }

    private void CheckBattleAfterPlayerAction()
    {
        if (enemyUnit.IsDead)
        {
            state = BattleState.Win;
            StartCoroutine(BattleOverRoutine(true));
        }
        else
        {
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    // ==========================================
    // [階段一 & 二] 敵人回合與行動流程 (EnemyTurn / EnemyAction)
    // ==========================================
    private IEnumerator EnemyTurnRoutine()
    {
        state = BattleState.EnemyTurn;
        Debug.Log($"<color=#FF5252><b>[BattleSystem]</b> 輪到敵人回合！狀態進入: [EnemyTurn] (敵人 HP: {enemyUnit.CurrentHP}/{enemyUnit.MaxHP})</color>");

        if (battleStatusText != null)
        {
            battleStatusText.text = $"【敵人回合】{enemyUnit.UnitName} 正在蓄力反擊...";
        }

        // 敵人等待思考 1.0 秒
        yield return new WaitForSeconds(1.0f);

        state = BattleState.EnemyAction;
        Debug.Log($"<color=#FF1744><b>[BattleSystem]</b> 敵人發動行動！狀態進入: [EnemyAction]</color>");

        if (battleStatusText != null)
        {
            battleStatusText.text = $"{enemyUnit.UnitName} 發起猛烈反擊！";
        }

        // 敵人發動近戰攻擊衝向玩家
        yield return StartCoroutine(enemyUnit.MeleeAttackCoroutine(playerUnit, () =>
        {
            bool isDead = playerUnit.TakeDamage(enemyUnit.Damage);
            UpdateHPUI();
            Debug.Log($"[BattleSystem] 玩家受到 {enemyUnit.Damage} 點傷害，當前 HP: {playerUnit.CurrentHP}/{playerUnit.MaxHP}");
        }));

        yield return new WaitForSeconds(0.5f);

        // 檢查玩家是否陣亡
        if (playerUnit.IsDead)
        {
            state = BattleState.Lose;
            StartCoroutine(BattleOverRoutine(false));
        }
        else
        {
            // 回到玩家回合
            EnterPlayerTurn();
        }
    }

    // ==========================================
    // [階段一] 戰鬥結束結算 (Win / Lose)
    // ==========================================
    private IEnumerator BattleOverRoutine(bool isWin)
    {
        SetButtonsInteractable(false);

        if (isWin)
        {
            Debug.Log("<color=#00E676><b>[BattleSystem] ===== 戰鬥獲勝！狀態進入: [Win] =====</b> 恭喜擊敗所有敵人！</color>");
            if (battleStatusText != null)
            {
                battleStatusText.text = "<color=#00FF66>【VICTORY 戰鬥勝利】\n成功擊敗對手！</color>";
            }
        }
        else
        {
            Debug.Log("<color=#D50000><b>[BattleSystem] ===== 戰鬥失敗... 狀態進入: [Lose] =====</b> 玩家已被擊倒！</color>");
            if (battleStatusText != null)
            {
                battleStatusText.text = "<color=#FF3333>【DEFEAT 戰鬥失敗】\n甲賀忍蛙失去了戰鬥能力...</color>";
            }
        }

        yield return null;
    }

    // ==========================================
    // [階段三] UI 輔助方法
    // ==========================================
    /// <summary>
    /// 同步更新玩家與敵人的血條 Slider 與文字
    /// </summary>
    public void UpdateHPUI()
    {
        if (playerUnit != null)
        {
            if (playerHPSlider != null)
            {
                playerHPSlider.maxValue = playerUnit.MaxHP;
                playerHPSlider.value = playerUnit.CurrentHP;
            }
            if (playerHPText != null)
            {
                playerHPText.text = $"{playerUnit.UnitName}\nHP: {playerUnit.CurrentHP} / {playerUnit.MaxHP}";
            }
        }

        if (enemyUnit != null)
        {
            if (enemyHPSlider != null)
            {
                enemyHPSlider.maxValue = enemyUnit.MaxHP;
                enemyHPSlider.value = enemyUnit.CurrentHP;
            }
            if (enemyHPText != null)
            {
                enemyHPText.text = $"{enemyUnit.UnitName}\nHP: {enemyUnit.CurrentHP} / {enemyUnit.MaxHP}";
            }
        }
    }

    /// <summary>
    /// 設定攻擊按鈕的互動狀態 (只有在 PlayerTurn 時為 true)
    /// </summary>
    private void SetButtonsInteractable(bool interactable)
    {
        if (meleeAttackButton != null)
        {
            meleeAttackButton.interactable = interactable;
        }
        if (rangedAttackButton != null)
        {
            rangedAttackButton.interactable = interactable;
        }
    }
}
