using UnityEngine;
using TMPro;

/// <summary>
/// 回合制戰鬥技能傷害與天氣連動測試腳本 (BattleWeatherTest)
/// 負責讀取 WeatherBattleManager 的即時屬性倍率，計算最終水屬性技能傷害，
/// 並透過 TextMeshProUGUI 顯示於畫面，支援 UI 按鈕點擊與 Inspector 右鍵快速測試。
/// </summary>
public class BattleWeatherTest : MonoBehaviour
{
    [Header("=== 技能數值設定 ===")]
    [Tooltip("水手裡劍基礎傷害 (Base Water Damage)")]
    [SerializeField] private float baseWaterDamage = 100f;

    [Header("=== 外部關聯 (若留空會自動透過 Singleton 尋找) ===")]
    [Tooltip("即時天氣倍率管理器 (WeatherBattleManager)")]
    [SerializeField] private WeatherBattleManager weatherManager;

    [Header("=== UI 顯示元件 ===")]
    [Tooltip("顯示技能釋放與傷害結算記錄的 TextMeshPro 元件")]
    [SerializeField] private TextMeshProUGUI battleLogText;

    private void Start()
    {
        // 自動嘗試尋找 WeatherBattleManager
        if (weatherManager == null)
        {
            weatherManager = WeatherBattleManager.Instance;
            if (weatherManager == null)
            {
                weatherManager = FindFirstObjectByType<WeatherBattleManager>();
            }
        }
    }

    /// <summary>
    /// 釋放水手裡劍技能 (供 Unity UI 按鈕 OnClick 事件綁定)
    /// </summary>
    [ContextMenu("Cast Water Shuriken")]
    public void CastWaterShuriken()
    {
        // 確保取得 WeatherBattleManager
        if (weatherManager == null)
        {
            weatherManager = WeatherBattleManager.Instance ?? FindFirstObjectByType<WeatherBattleManager>();
        }

        // 讀取即時水屬性加成倍率
        float multiplier = (weatherManager != null) ? weatherManager.WaterDamageMultiplier : 1.0f;
        string weatherStr = (weatherManager != null) ? weatherManager.CurrentWeatherEnglish : "Normal";

        // 計算最終傷害: 最終傷害 = 基礎傷害 * 水屬性倍率
        float finalDamage = baseWaterDamage * multiplier;

        // 格式化輸出文字
        string logMsg = $"Cast Water Shuriken! Weather: {weatherStr} -> Final DMG: {finalDamage:F0}";

        Debug.Log($"<color=cyan><b>[BattleWeatherTest]</b> {logMsg} (Base: {baseWaterDamage} x {multiplier})</color>");

        // 更新畫面上的 UI 文字
        if (battleLogText != null)
        {
            battleLogText.text = logMsg;

            // 依據是否有 Buff 調整文字顏色
            if (multiplier > 1.0f)
            {
                battleLogText.color = Color.cyan;
            }
            else if (multiplier < 1.0f)
            {
                battleLogText.color = new Color(0.8f, 0.8f, 0.8f);
            }
            else
            {
                battleLogText.color = Color.white;
            }
        }
    }
}
