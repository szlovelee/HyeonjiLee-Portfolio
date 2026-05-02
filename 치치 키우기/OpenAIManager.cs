using UnityEngine;
using OpenAI;
using OpenAI.Chat;

public partial class OpenAIManager : Singleton<OpenAIManager>
{
    private const string MODEL = "gpt-4o";
    private const string KEY = "YOUR_API_KEY";

    private OpenAIClient _client;
    private ChatService _chat;

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Initialize()
    {
        _client = new OpenAIClient(KEY);
        _chat = new ChatService(_client.GetChatClient(MODEL));
    }

    public string GetModel() => MODEL;

    public void SendChatMessage(string message, out string answer)
    {
        _chat.SendMessage(message, out ResponseContents contents);

        answer = contents.Equals(default)
            ? "응답이 없습니다."
            : contents.Response;

        if (!contents.Equals(default))
        {
            OnEmotionReceived(contents.Emotion);
            OnAdviceTagReceived(contents.AdviceTag);
        }
    }

    private void OnEmotionReceived(string emotion) { }
    private void OnAdviceTagReceived(string adviceTag) { }
}
