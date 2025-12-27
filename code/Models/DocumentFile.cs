using CommunityToolkit.Mvvm.ComponentModel;

namespace DocMind
{
    public partial class DocumentFile : ObservableObject
    {
        [ObservableProperty]
        private string _fileName = "";

        [ObservableProperty]
        private string? _filePath;

        [ObservableProperty]
        private long _fileSize;

        [ObservableProperty]
        private string? _content;

        [ObservableProperty]
        private object? _display;

        [ObservableProperty]
        private double _fileSizeKB;

        public required object FileMeta { get; set; }

        public required FileType FileType { get; set; }
    }
}
