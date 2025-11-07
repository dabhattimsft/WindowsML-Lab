using System;
using System.ComponentModel;

namespace WinMLLabDemo
{
    public class ChatMessage : INotifyPropertyChanged
    {
        private string _message;
        public string Message 
        { 
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
                }
            }
        }

        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ChatMessage(string message, bool isUser)
        {
            _message = message;
            IsUser = isUser;
            Timestamp = DateTime.Now;
        }
    }
}