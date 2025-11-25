using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;

public class MessageBlock : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public RectTransform messageBlockRectTransform;
    public float x_padding = 20f;
    public float y_padding = 10f;
    public float maxWidth = 500f; // Set to 0 for auto width

    void Start()
    {
        Vector2 textSize = messageText.GetPreferredValues();

        float width = textSize.x + 2 * x_padding;
        if (maxWidth > 0 && width > maxWidth)
        {
            width = maxWidth;
        }
        messageBlockRectTransform.sizeDelta = new Vector2(width, textSize.y + 2 * y_padding);
    }

    void Update()
    {
        Vector2 textSize = messageText.GetPreferredValues();

        float width = textSize.x + 2 * x_padding;
        if (maxWidth > 0 && width > maxWidth)
        {
            width = maxWidth;
        }
        messageBlockRectTransform.sizeDelta = new Vector2(width, textSize.y + 2 * y_padding);
    }

    void OnValidate()
    {
        Vector2 textSize = messageText.GetPreferredValues();

        float width = textSize.x + 2 * x_padding;
        if (maxWidth > 0 && width > maxWidth)
        {
            width = maxWidth;
        }

        Debug.Log($"Text Width: {textSize.x}, Text Height: {textSize.y}");
        messageBlockRectTransform.sizeDelta = new Vector2(width, textSize.y + 2 * y_padding);
    }
}
