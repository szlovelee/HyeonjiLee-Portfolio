using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatMessageBox : MonoBehaviour
{
    [SerializeField] private RectTransform entityArea;
    [SerializeField] private RectTransform boxArea;
    [SerializeField] private TextMeshProUGUI textArea;

    public void SetMessage(string message)
    {
        textArea.text = message;
        AdjustBoxSize();
    }

    private void AdjustBoxSize()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(textArea.rectTransform);

        float height = textArea.rectTransform.rect.height + 50f;
        boxArea.sizeDelta   = new Vector2(boxArea.rect.width, height);
        entityArea.sizeDelta = new Vector2(entityArea.rect.width, height);
    }
}
