using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class KeyboardFollower : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 originalPos;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        originalPos = rect.anchoredPosition;
    }

    void Update()
    {
        if (TouchScreenKeyboard.visible)
        {
            float keyboardHeight = Screen.height - Display.main.systemHeight;

            // fallback for Android
            if (keyboardHeight == 0)
                keyboardHeight = Screen.height * 0.35f;

            keyboardHeight /= CanvasScale();

            rect.anchoredPosition = originalPos + new Vector2(0, keyboardHeight);
        }
        else
        {
            rect.anchoredPosition = originalPos;
        }
    }

    float CanvasScale()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            return canvas.scaleFactor;

        return 1f;
    }
}