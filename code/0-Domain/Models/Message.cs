using CommunityToolkit.Mvvm.ComponentModel;

namespace DocMind
{
    public partial class Message : ObservableObject
    {
        [ObservableProperty]
        private string _text = "";

        [ObservableProperty]
        private bool _isSentByUser;

        [ObservableProperty]
        private DateTime _timestamp = DateTime.Now;

        [ObservableProperty]
        private bool _isStreamingFinished;
    }
}
