using CommunityToolkit.Mvvm.ComponentModel;
using DocMind.code.Infrastructure;
using System.Windows.Documents;

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

        //[ObservableProperty]
        //private FlowDocument? _flowDocumentContent;

        //partial void OnTextChanged(string? oldValue, string? newValue)
        //{
        //    FlowDocumentContent = FlowDocumentHelper.ConvertToFlowDocument(newValue ?? "");
        //}
    }
}
