using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIChat : MonoBehaviour
{
    [SerializeField] private TMP_InputField clientInput;
    [SerializeField] private Button sendBtn;

    [SerializeField] private RectTransform contentArea;
    [SerializeField] private ChatMessageBox c_messageBox;
    [SerializeField] private ChatMessageBox a_messageBox;

    [SerializeField] private TextMeshProUGUI emotionTxt;
    [SerializeField] private TextMeshProUGUI adviceTagTxt;

    private OpenAIManager _manager;

    private void Start()
    {
        _manager = OpenAIManager.Instance;
        sendBtn.onClick.AddListener(SendMessage);
    }

    private void SendMessage()
    {
        string input = clientInput.text;
        if (string.IsNullOrWhiteSpace(input)) return;

        Instantiate(c_messageBox, contentArea).SetMessage(input);

        _manager.SendChatMessage(input, out string answer);

        Instantiate(a_messageBox, contentArea).SetMessage(answer);

        clientInput.text = string.Empty;
    }
}
