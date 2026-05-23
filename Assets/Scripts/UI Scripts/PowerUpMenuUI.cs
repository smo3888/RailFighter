using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class PowerUpMenuUI : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject menuPanel;
    public Button closeButton;
    public Button powersOpenButton;

    [Header("Offensive Slot (Left)")]
    public Image offensiveSlotIcon;
    public Button offensiveSlotButton;

    [Header("Defensive Slot (Right)")]
    public Image defensiveSlotIcon;
    public Button defensiveSlotButton;

    [Header("Collection Grid")]
    public PowerUpCollectionUI collectionUI;

    [Header("Pixelate (same as PauseMenu)")]
    public ScriptableRendererData rendererData;
    [Range(1f, 20f)]
    public float targetPixelSize = 8f;
    public float animationDuration = 0.3f;

    private bool isOpen = false;
    private FullScreenPassRendererFeature pixelateFeature;

    void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (powersOpenButton != null) powersOpenButton.onClick.AddListener(Open);
        if (offensiveSlotButton != null) offensiveSlotButton.onClick.AddListener(() => OpenCollection(PowerUpType.Offensive));
        if (defensiveSlotButton != null) defensiveSlotButton.onClick.AddListener(() => OpenCollection(PowerUpType.Defensive));

        if (rendererData != null)
        {
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is FullScreenPassRendererFeature fsPass)
                {
                    pixelateFeature = fsPass;
                    break;
                }
            }
        }

        SetPixelateActive(false);
        Shader.SetGlobalFloat("_PixelSize", 1f);
        Shader.SetGlobalFloat("_MouseRadius", 0.15f);
        Shader.SetGlobalFloat("_DistortionStrength", 1.5f);
        Shader.SetGlobalVector("_MouseUV", Vector2.zero);

        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.OnEquipmentChanged += RefreshSlots;
    }

    void OnDestroy()
    {
        if (PowerUpManager.Instance != null)
            PowerUpManager.Instance.OnEquipmentChanged -= RefreshSlots;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) Open();
        if (isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();

        if (isOpen)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 mouseUV = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);
            Shader.SetGlobalVector("_MouseUV", mouseUV);
            Shader.SetGlobalFloat("_Time2", Time.unscaledTime);
        }
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (menuPanel != null) menuPanel.SetActive(true);
        SetPixelateActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimatePixelate(1f, targetPixelSize, () => { Time.timeScale = 0f; }));
        RefreshSlots();
    }

    public void Close()
    {
        if (!isOpen) return;
        Time.timeScale = 1f;
        isOpen = false;
        collectionUI?.Close();
        if (menuPanel != null) menuPanel.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(AnimatePixelate(targetPixelSize, 1f, () => { SetPixelateActive(false); }));
    }

    void OpenCollection(PowerUpType slotType)
    {
        if (collectionUI == null) return;
        collectionUI.Open(slotType, this);
    }

    public void RefreshSlots()
    {
        if (PowerUpManager.Instance == null) return;
        RefreshSlot(PowerUpManager.Instance.GetEquippedOffensive(), offensiveSlotIcon);
        RefreshSlot(PowerUpManager.Instance.GetEquippedDefensive(), defensiveSlotIcon);
    }

    void RefreshSlot(PowerUpData data, Image icon)
    {
        Debug.Log($"[RefreshSlot] data={data?.displayName ?? "NULL"}, icon field={(icon != null ? icon.name : "NULL FIELD")}, sprite on data={data?.icon?.name ?? "NO SPRITE"}");
        if (icon == null) return;
        icon.gameObject.SetActive(data != null);
        if (data != null)
        {
            icon.sprite = data.icon;
            icon.color = Color.white;
        }
    }

    public void SelectForEquip(PowerUpData data)
    {
        PowerUpManager.Instance?.EquipPowerUp(data);
        RefreshSlots();
    }

    public void UnequipSlot(PowerUpType slotType)
    {
        PowerUpManager.Instance?.UnequipSlot(slotType);
        RefreshSlots();
    }

    IEnumerator AnimatePixelate(float from, float to, System.Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animationDuration);
            Shader.SetGlobalFloat("_PixelSize", Mathf.Lerp(from, to, t));
            yield return null;
        }
        Shader.SetGlobalFloat("_PixelSize", to);
        onComplete?.Invoke();
    }

    void SetPixelateActive(bool active)
    {
        if (pixelateFeature != null) pixelateFeature.SetActive(active);
    }
}