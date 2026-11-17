using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// 即時天氣連動回合制屬性加成管理器 (WeatherBattleManager)
/// 負責透過 CWA API (O-A0003-001) 或本機 weather.json 獲取氣象資料，
/// 並計算水/火屬性技能傷害倍率，同時更新 TextMeshPro UI（英文顯示以避開缺字問題）。
/// </summary>
public class WeatherBattleManager : MonoBehaviour
{
    public static WeatherBattleManager Instance { get; private set; }

    // ==========================================
    // Inspector 參數設定
    // ==========================================
    [Header("=== CWA API 連線設定 ===")]
    [Tooltip("中央氣象署開放資料平臺授權碼 (API Key)，若無請勾選 useLocalFile 使用本機測試檔")]
    [SerializeField] private string apiKey = "";

    [Tooltip("測站名稱 (官方預設為 '臺北'，注意需為繁體『臺』)")]
    [SerializeField] private string stationName = "臺北";

    [Header("=== 本機測試檔案設定 ===")]
    [Tooltip("勾選時從 Application.streamingAssetsPath + '/weather.json' 讀取，方便離線或模擬測試")]
    [SerializeField] private bool useLocalFile = false;

    [Header("=== 輪詢頻率設定 ===")]
    [Tooltip("定期輪詢間隔 (秒)，預設 300 秒以符合氣象署更新規範")]
    [SerializeField] private float pollInterval = 300f;

    [Header("=== UI 顯示元件 ===")]
    [Tooltip("顯示於畫面上的天氣與屬性加成資訊 UI")]
    [SerializeField] private TextMeshProUGUI weatherStatusText;

    // ==========================================
    // 回合制屬性倍率 (公開供戰鬥系統讀取)
    // ==========================================
    [Header("=== 即時屬性加成倍率 (唯讀觀察) ===")]
    [SerializeField] private float currentWaterMultiplier = 1.0f;
    [SerializeField] private float currentFireMultiplier = 1.0f;

    public float WaterDamageMultiplier => currentWaterMultiplier;
    public float FireDamageMultiplier => currentFireMultiplier;

    // 氣象資料快取 (純英文以避免 TMP 方塊字)
    public string CurrentWeatherEnglish { get; private set; } = "Normal";
    public string CurrentStationEnglish { get; private set; } = "Taipei";
    public float CurrentTemperature { get; private set; }
    public float CurrentHumidity { get; private set; }

    private Coroutine pollCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 啟動定時輪詢協程
        pollCoroutine = StartCoroutine(WeatherPollingRoutine());
    }

    private void OnDestroy()
    {
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
        }
    }

    // ==========================================
    // 定時輪詢與資料擷取協程
    // ==========================================
    private IEnumerator WeatherPollingRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(FetchWeatherRoutine());
            yield return new WaitForSeconds(Mathf.Max(10f, pollInterval));
        }
    }

    /// <summary>
    /// 右鍵點選 Inspector 元件標題可手動立即刷新
    /// </summary>
    [ContextMenu("Refresh Now")]
    public void RefreshNow()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(FetchWeatherRoutine());
        }
        else
        {
            LoadLocalFileDirectly();
        }
    }

    /// <summary>
    /// 獲取氣象資料核心協程 (依設定決定從網路 API 或本機檔案讀取)
    /// </summary>
    public IEnumerator FetchWeatherRoutine()
    {
        string jsonText = null;

        // 1. 若勾選本機測試或無 API Key，直接讀取 StreamingAssets/weather.json
        if (useLocalFile || string.IsNullOrEmpty(apiKey))
        {
            if (string.IsNullOrEmpty(apiKey) && !useLocalFile)
            {
                Debug.LogWarning("[WeatherBattleManager] 未填入 CWA API Key，自動切換為讀取本機 weather.json 測試檔。");
            }

            string localPath = Path.Combine(Application.streamingAssetsPath, "weather.json");
            if (File.Exists(localPath))
            {
                jsonText = File.ReadAllText(localPath);
            }
            else
            {
                // WebGL / Android 等平台的 UnityWebRequest 讀取途徑
                using (UnityWebRequest req = UnityWebRequest.Get(localPath))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        jsonText = req.downloadHandler.text;
                    }
                    else
                    {
                        Debug.LogError($"[WeatherBattleManager] 無法讀取本機檔案: {localPath} | 錯誤: {req.error}");
                    }
                }
            }
        }
        else
        {
            // 2. 透過 CWA 官方 API 網址請求
            string url = $"https://opendata.cwa.gov.tw/api/v1/rest/datastore/O-A0003-001?Authorization={apiKey}&StationName={UnityWebRequest.EscapeURL(stationName)}";
            Debug.Log($"[WeatherBattleManager] 正在向中央氣象署請求即時天氣... ({stationName})");

            using (UnityWebRequest webReq = UnityWebRequest.Get(url))
            {
                webReq.timeout = 10;
                yield return webReq.SendWebRequest();

                if (webReq.result == UnityWebRequest.Result.Success)
                {
                    jsonText = webReq.downloadHandler.text;
                }
                else
                {
                    Debug.LogError($"[WeatherBattleManager] CWA API 請求失敗: {webReq.error}，嘗試切換至本機備援檔案...");
                    string localPath = Path.Combine(Application.streamingAssetsPath, "weather.json");
                    if (File.Exists(localPath))
                    {
                        jsonText = File.ReadAllText(localPath);
                    }
                }
            }
        }

        // 3. 解析 JSON 資料
        if (!string.IsNullOrEmpty(jsonText))
        {
            ParseAndApplyWeatherData(jsonText);
        }
        else
        {
            Debug.LogWarning("[WeatherBattleManager] 未取得任何有效天氣 JSON 字串。");
        }
    }

    /// <summary>
    /// 解析 CWA JSON 並套用天氣倍率與 UI
    /// </summary>
    private void ParseAndApplyWeatherData(string json)
    {
        try
        {
            CwaResponse response = JsonUtility.FromJson<CwaResponse>(json);

            if (response != null && response.records != null && response.records.Station != null && response.records.Station.Count > 0)
            {
                Station station = response.records.Station[0];
                ApplyStationData(station);
            }
            else
            {
                Debug.LogError("[WeatherBattleManager] JSON 解析成功但未找到有效的 Station 陣列資料！");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WeatherBattleManager] JSON 解析異常: {ex.Message}\n內容: {json}");
        }
    }

    /// <summary>
    /// 編輯模式下直接同步讀取本機檔案以利預覽
    /// </summary>
    private void LoadLocalFileDirectly()
    {
        string localPath = Path.Combine(Application.streamingAssetsPath, "weather.json");
        if (File.Exists(localPath))
        {
            string json = File.ReadAllText(localPath);
            ParseAndApplyWeatherData(json);
            Debug.Log("<color=green>[WeatherBattleManager] 本機 weather.json 讀取並套用成功！</color>");
        }
        else
        {
            Debug.LogError($"[WeatherBattleManager] 找不到本機檔案: {localPath}");
        }
    }

    /// <summary>
    /// 計算屬性加成倍率、變換 UI 文字與顏色
    /// </summary>
    private void ApplyStationData(Station station)
    {
        string rawWeather = station.WeatherElement != null ? station.WeatherElement.Weather : "";
        CurrentTemperature = station.WeatherElement != null ? station.WeatherElement.AirTemperature : 0f;
        CurrentHumidity = station.WeatherElement != null ? station.WeatherElement.RelativeHumidity : 0f;

        // 依照避坑規範：轉換為純英文顯示，避免 TMP 方塊缺字
        CurrentWeatherEnglish = ConvertWeatherToEnglish(rawWeather);
        CurrentStationEnglish = ConvertStationToEnglish(station.StationName);

        string buffNotice;
        Color statusColor;

        // ------------------------------------------
        // 屬性倍率判定規則 (依規格書定義)
        // ------------------------------------------
        if (rawWeather != null && rawWeather.Contains("雨"))
        {
            // 雨天 (Rainy)：水屬性 1.5 倍，火屬性 0.8 倍，青藍色
            currentWaterMultiplier = 1.5f;
            currentFireMultiplier = 0.8f;
            buffNotice = "[BUFF] Water Skill DMG x1.5! (Fire x0.8)";
            statusColor = Color.cyan;
        }
        else if (rawWeather != null && rawWeather.Contains("晴"))
        {
            // 晴天 (Sunny)：火屬性 1.5 倍，水屬性 0.8 倍，橘紅色
            currentFireMultiplier = 1.5f;
            currentWaterMultiplier = 0.8f;
            buffNotice = "[BUFF] Fire Skill DMG x1.5! (Water x0.8)";
            statusColor = new Color(1f, 0.4f, 0f);
        }
        else
        {
            // 其他 (陰、多雲、Normal)：無加成，皆為 1.0 倍，白色
            currentWaterMultiplier = 1.0f;
            currentFireMultiplier = 1.0f;
            buffNotice = "[NORMAL] Weather DMG x1.0";
            statusColor = Color.white;
        }

        // ------------------------------------------
        // 畫面 UI 更新 (純英文排版，絕對安全不缺字)
        // ------------------------------------------
        if (weatherStatusText != null)
        {
            weatherStatusText.color = statusColor;
            weatherStatusText.text =
                $"<b>[Digital Twin Weather]</b>\n" +
                $"Station: {CurrentStationEnglish} | Weather: {CurrentWeatherEnglish}\n" +
                $"Temp: {CurrentTemperature:F1} C | Humidity: {CurrentHumidity:F0}%\n" +
                $"<size=110%>{buffNotice}</size>";
        }

        Debug.Log($"<color={(statusColor == Color.white ? "white" : (statusColor == Color.cyan ? "#00E5FF" : "#FF9100"))}>" +
                  $"[WeatherBattleManager] 氣象更新完成: {CurrentStationEnglish} - {CurrentWeatherEnglish} " +
                  $"(氣溫: {CurrentTemperature}C, 濕度: {CurrentHumidity}%) => {buffNotice}</color>");
    }

    // ==========================================
    // 字型避坑轉換函式 (中文字元轉為純英文)
    // ==========================================
    /// <summary>
    /// 將 CWA 中文天氣描述轉換為安全英文字串
    /// 包含「雨」-> Rainy, 包含「晴」-> Sunny, 包含「陰」或「雲」-> Cloudy, 其餘 -> Normal
    /// </summary>
    public static string ConvertWeatherToEnglish(string cwaWeather)
    {
        if (string.IsNullOrEmpty(cwaWeather)) return "Normal";

        if (cwaWeather.Contains("雨")) return "Rainy";
        if (cwaWeather.Contains("晴")) return "Sunny";
        if (cwaWeather.Contains("陰") || cwaWeather.Contains("雲")) return "Cloudy";

        return "Normal";
    }

    /// <summary>
    /// 測站中文轉換為英文
    /// </summary>
    public static string ConvertStationToEnglish(string cwaStation)
    {
        if (string.IsNullOrEmpty(cwaStation)) return "Taipei";

        if (cwaStation.Contains("臺北") || cwaStation.Contains("台北")) return "Taipei";
        if (cwaStation.Contains("新北")) return "New Taipei";
        if (cwaStation.Contains("桃園")) return "Taoyuan";
        if (cwaStation.Contains("臺中") || cwaStation.Contains("台中")) return "Taichung";
        if (cwaStation.Contains("臺南") || cwaStation.Contains("台南")) return "Tainan";
        if (cwaStation.Contains("高雄")) return "Kaohsiung";
        if (cwaStation.Contains("花蓮")) return "Hualien";

        return "Taipei Station";
    }
}
