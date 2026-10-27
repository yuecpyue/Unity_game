using UnityEngine;

/// <summary>
/// 掛載在物件上，提供標題與詳細描述資訊。
/// </summary>
public class InfoTag : MonoBehaviour
{
    [Header("資訊內容")]
    [Tooltip("物件標題")]
    public string title = "";

    [Tooltip("物件詳細說明")]
    [TextArea(3, 8)]
    public string description = "";
}
