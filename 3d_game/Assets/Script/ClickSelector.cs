using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 左鍵點選帶有 Collider 的物件：
/// - 若有 InfoTag 則顯示黃色高亮，並在 TextMeshProUGUI 顯示資訊。
/// - 再次點擊同一物件取消選取。
/// - 點擊其他 InfoTag 物件則先還原前一個再選取新物件。
/// - 點擊空白處或未帶有 InfoTag 的物件則取消選取。
/// </summary>
public class ClickSelector : MonoBehaviour
{
    [Header("UI 關聯")]
    [Tooltip("用於顯示資訊的 TextMeshProUGUI 組件")]
    public TextMeshProUGUI infoText;

    [Header("高亮設定")]
    [Tooltip("選取時的高亮顏色")]
    public Color highlightColor = Color.yellow;

    [Header("射線設定")]
    [Tooltip("偵測所用的攝影機，留空則自動使用 Camera.main")]
    public Camera targetCamera;

    [Tooltip("射線碰撞遮罩層")]
    public LayerMask hitLayers = ~0;

    private InfoTag currentSelectedTag;
    private readonly List<Renderer> currentRenderers = new List<Renderer>();
    private MaterialPropertyBlock highlightBlock;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        highlightBlock = new MaterialPropertyBlock();
        highlightBlock.SetColor("_BaseColor", highlightColor);
        highlightBlock.SetColor("_Color", highlightColor);
    }

    private void Start()
    {
        ClearSelection();
    }

    private void Update()
    {
        // 1. 新版 Input System 檢查滑鼠左鍵點擊
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        // 2. 避免穿透 UI 點擊 3D 場景
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 3. 確保攝影機存在
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        // 4. 發射射線偵測 3D 物件
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, hitLayers))
        {
            // 嘗試從擊中的 Collider 或其父物件尋找 InfoTag
            InfoTag hitTag = hit.collider.GetComponentInParent<InfoTag>();

            if (hitTag != null)
            {
                if (hitTag == currentSelectedTag)
                {
                    // 再次點擊同一物件 -> 取消選取
                    ClearSelection();
                }
                else
                {
                    // 點擊其他 InfoTag 物件 -> 先還原前一個，再選取新物件
                    SelectTag(hitTag);
                }
            }
            else
            {
                // 點擊到沒有 InfoTag 的 Collider -> 取消選取
                ClearSelection();
            }
        }
        else
        {
            // 點擊空白處 -> 取消選取
            ClearSelection();
        }
    }

    /// <summary>
    /// 選取指定的 InfoTag 物件並套用高亮與文字顯示
    /// </summary>
    private void SelectTag(InfoTag tag)
    {
        // 還原上一個被選取物件的材質
        RestoreCurrentRenderers();

        currentSelectedTag = tag;

        // 取得該物件及其所有子物件上的所有 Renderer
        currentRenderers.Clear();
        tag.GetComponentsInChildren(true, currentRenderers);

        highlightBlock.SetColor("_BaseColor", highlightColor);
        highlightBlock.SetColor("_Color", highlightColor);

        for (int i = 0; i < currentRenderers.Count; i++)
        {
            if (currentRenderers[i] != null)
            {
                currentRenderers[i].SetPropertyBlock(highlightBlock);
            }
        }

        // 更新 UI 顯示
        UpdateInfoDisplay(tag);
    }

    /// <summary>
    /// 取消當前選取，還原外觀並清空文字
    /// </summary>
    public void ClearSelection()
    {
        RestoreCurrentRenderers();
        currentSelectedTag = null;

        if (infoText != null)
        {
            infoText.text = string.Empty;
        }
    }

    /// <summary>
    /// 清除所有套用至目前物件的 MaterialPropertyBlock 高亮效果
    /// </summary>
    private void RestoreCurrentRenderers()
    {
        for (int i = 0; i < currentRenderers.Count; i++)
        {
            if (currentRenderers[i] != null)
            {
                currentRenderers[i].SetPropertyBlock(null);
            }
        }
        currentRenderers.Clear();
    }

    /// <summary>
    /// 將 InfoTag 的資訊顯示在 TextMeshProUGUI 上
    /// </summary>
    private void UpdateInfoDisplay(InfoTag tag)
    {
        if (infoText == null) return;

        if (!string.IsNullOrEmpty(tag.description))
        {
            infoText.text = $"<b>{tag.title}</b>\n{tag.description}";
        }
        else
        {
            infoText.text = tag.title;
        }
    }
}
