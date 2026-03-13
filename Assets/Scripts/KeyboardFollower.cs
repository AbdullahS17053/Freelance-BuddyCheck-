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
            Rect keyboardArea = TouchScreenKeyboard.area;

            if (keyboardArea.height > 0)
            {
                float keyboardHeight = keyboardArea.height / CanvasScale();
                rect.anchoredPosition = originalPos + new Vector2(0, keyboardHeight);
            }
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