using System;

namespace WinMLLabDemo
{
    public class ChatMessage
    {
        public string Message { get; set; }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }

        public ChatMessage(string message, bool isUser)
        {
            Message = message;
            IsUser = isUser;
            Timestamp = DateTime.Now;
        }
    }
}