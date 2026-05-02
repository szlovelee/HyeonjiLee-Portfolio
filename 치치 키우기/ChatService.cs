using System;
using System.Collections.Generic;
using System.Text.Json;
using OpenAI.Chat;
using UnityEngine;

public partial class OpenAIManager
{
    private class ChatService
    {
        private readonly ChatClient _client;

        public ChatService(ChatClient client)
        {
            _client = client;
        }

        public void SendMessage(string userMessage, out ResponseContents contents)
        {
            contents = default;

            try
            {
                List<ChatMessage> messages = new List<ChatMessage>
                {
                    new SystemChatMessage(
                        "너는 감정 케어 AI 캐릭터 '치치'야. " +
                        "항상 따뜻한 어조로 대화하며, 사용자의 감정을 이해하려 노력해. " +
                        "모든 응답은 반드시 JSON 형식으로만 반환해. " +
                        "응답 외의 설명 문장은 절대 포함하지 마."
                    ),
                    new UserChatMessage(
                        $"사용자가 말했다: \"{userMessage}\". " +
                        "이 발화를 분석해서 다음 형식의 JSON으로 반환해:\n\n" +
                        "{\n" +
                        "  \"emotion\": \"감정 이름\",\n" +
                        "  \"response\": \"치치의 대답\",\n" +
                        "  \"advice_tag\": \"간단 조언 태그\"\n" +
                        "}\n\n" +
                        "JSON 외의 다른 텍스트는 절대 출력하지 마."
                    )
                };

                ChatCompletionOptions options = new ChatCompletionOptions
                {
                    Temperature = 0.7f
                };

                ChatCompletion completion = _client.CompleteChat(messages, options);
                string content = completion.Content[0].Text.Trim();

                content = StripMarkdownCodeBlock(content);

                using JsonDocument doc = JsonDocument.Parse(content);
                JsonElement root = doc.RootElement;

                contents = new ResponseContents
                {
                    Emotion   = root.GetProperty("emotion").GetString(),
                    Response  = root.GetProperty("response").GetString(),
                    AdviceTag = root.TryGetProperty("advice_tag", out var tag) ? tag.GetString() : "none"
                };
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[ChatService] JSON 파싱 실패: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatService] 요청 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// GPT가 응답에 마크다운 코드블록(```json ... ```)을 삽입하는 경우 제거한다.
        /// </summary>
        private static string StripMarkdownCodeBlock(string content)
        {
            if (!content.StartsWith("```")) return content;

            int start = content.IndexOf("```", StringComparison.Ordinal);
            int end   = content.LastIndexOf("```", StringComparison.Ordinal);

            if (start != -1 && end != -1 && end > start)
                content = content.Substring(start + 3, end - start - 3);

            return content.Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();
        }
    }

    public struct ResponseContents
    {
        public string Emotion;
        public string Response;
        public string AdviceTag;
    }
}
