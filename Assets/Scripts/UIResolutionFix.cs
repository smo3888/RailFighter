using UnityEngine;
using UnityEngine.UIElements;

public class UIResolutionFix : MonoBehaviour
{
    private UIDocument uiDocument;
    private int lastWidth;
    private int lastHeight;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    void Update()
    {
        // Check if resolution changed
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            // Force UI rebuild
            RefreshUI();
        }
    }

    void RefreshUI()
    {
        if (uiDocument != null && uiDocument.panelSettings != null)
        {
            // Force panel to recalculate
            var root = uiDocument.rootVisualElement;
            root.MarkDirtyRepaint();
        }
    }
}