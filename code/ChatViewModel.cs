using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;

namespace DocMind
{
    //test push
    public partial class ChatViewModel : ObservableObject
    {
        //Field
        private readonly IMessenger _messenger = WeakReferenceMessenger.Default;
        private readonly IRagCoreService _ragCoreService;
        private readonly IDocumentLoaderService _documentLoaderService;

        //MVVM
        public ObservableCollection<DocumentFile> UploadedFiles { get; } = [];
        public ObservableCollection<Message> Messages { get; } = [];

        [ObservableProperty]
        private string _userQuestion = "输入消息...";

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isTraking = false;

        [ObservableProperty]
        private bool _isSourcingEnabled = false;

        [ObservableProperty]
        private DocumentFile? _selectedFile;

        [ObservableProperty]
        private string _hint = "";

        //Ctor
        public ChatViewModel(IRagCoreService ragCoreService, IDocumentLoaderService documentLoaderService)
        {
            _ragCoreService = ragCoreService;
            _documentLoaderService = documentLoaderService;

            InitChatMessage();
        }

        #region Private

        private void InitChatMessage()
        {
            Messages.Add(new Message
            {
                Text = "你好！我是基于GPT的助手，有什么我能帮你的吗？",
                IsSentByUser = false,
                Timestamp = DateTime.Now
            });
        }

        private static async Task OnUIAsync(Delegate method)
        {
            await Application.Current.Dispatcher.BeginInvoke(method);
        }

        private async Task ShowHint(string hint)
            => await OnUIAsync(() => Hint = hint);

        private async Task SendUserMessage(string str)
        {
            await OnUIAsync(() =>
            {
                Messages.Add(new Message
                {
                    Text = str,
                    IsSentByUser = true,
                    Timestamp = DateTime.Now
                });
            });
        }

        private async Task SendSysMessage(string str)
        {
            await OnUIAsync(() =>
            {
                Messages.Add(new Message
                {
                    Text = str,
                    IsSentByUser = false,
                    Timestamp = DateTime.Now
                });
            });
        }

        #endregion

        #region Command

        [RelayCommand]
        private async Task UploadFile()
        {
            try
            {
                await OnUIAsync(() => IsLoading = true);

                string? path = null;

                await OnUIAsync(() =>
                {
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "所有文件 (*.*)|*.*|Word文件 (*.docx;*.doc)|*.docx;*.doc|文本文件 (*.txt)|*.txt",
                        Multiselect = false,
                        Title = "选择文件（支持TXT以及WORD文档）"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        path = openFileDialog.FileName;
                    }
                });

                if (string.IsNullOrWhiteSpace(path))
                    return;

                if (UploadedFiles.Any(a => a.FilePath == path))
                {
                    MessageBox.Show($"文件已上传：{Path.GetFileName(path)}", "提示");
                    return;
                }

                var docFile = await _documentLoaderService.LoadFromFileAsync(path);

                await _ragCoreService.ImportDocumentAsync(Path.GetFileName(path), docFile.FileMeta);

                await SendSysMessage($"已成功导入文件：{docFile.FileName}({Path.GetExtension(path).ToUpper()})");

                await OnUIAsync(() =>
                {
                    UploadedFiles.Add(docFile);
                    SelectedFile ??= docFile;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"文件导入失败：{ex.Message}", "错误");
            }
            finally
            {
                await OnUIAsync(() => IsLoading = false);
            }
        }

        [RelayCommand]
        private void RemoveFile(DocumentFile file)
        {
            if (file != null)
            {
                UploadedFiles.Remove(file);
                Messages.Add(new Message
                {
                    Text = $"已移除文件：{file.FileName}",
                    IsSentByUser = false,
                    Timestamp = DateTime.Now
                });

                if (SelectedFile != null && file.FilePath == SelectedFile.FilePath)
                    SelectedFile = null;
            }
        }

        [RelayCommand]
        private void SelectFile(DocumentFile file)
        {
            SelectedFile = file;
        }

        [RelayCommand]
        private void JumpToPage(object parameter)
        {
            if (parameter is int targetPage)
            {
                _messenger.Send(new JumpPageMessage() { PageNumber = targetPage });
            }
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            try
            {
                //校验
                var userQuestion = UserQuestion.Trim();

                if (string.IsNullOrEmpty(userQuestion))
                    return;

                await SendUserMessage(userQuestion);

                if (SelectedFile == null)
                {
                    await SendSysMessage("请先选择文档");
                    return;
                }

                //获取Context
                var aiMessage = new Message
                {
                    Text = "...",
                    IsSentByUser = false
                };

                await OnUIAsync(() => Messages.Add(aiMessage));

                await ShowHint("正在生成回复");

                var context = await _ragCoreService.GetOriginContext(userQuestion, SelectedFile.FileType);

                if (string.IsNullOrWhiteSpace(context))
                {
                    await SendSysMessage("知识库检索失败，未能找到相关的上下文信息。");
                    return;
                }

                var firstAnswer = new StringBuilder();

                //第一次Chat
                await Task.Run(async () =>
                {
                    await foreach (var token in _ragCoreService.firstChat_Stream(userQuestion, context, SelectedFile.FileType).ConfigureAwait(false))
                    {
                        await OnUIAsync(() =>
                        {
                            if (aiMessage.Text == "...")
                                aiMessage.Text = token;
                            else
                                aiMessage.Text += token;
                        });

                        firstAnswer.Append(token);
                    }

                    if (SelectedFile.FileType == FileType.Word)
                        await OnUIAsync(() => aiMessage.IsStreamingFinished = true);
                });

                //启用溯源后 - 第二次chat
                if (IsSourcingEnabled)
                {
                    await ShowHint("正在生成溯源");

                    await OnUIAsync(() => IsTraking = true);

                    var secondAnswer = await _ragCoreService.secondChat(context, firstAnswer.ToString(), SelectedFile.FileType);

                    if (!string.IsNullOrWhiteSpace(secondAnswer))
                        _messenger.Send(new HighlightMessage(SelectedFile.FileName) { SecondAnswer = secondAnswer });
                }

            }
            catch (Exception ex)
            {
                await SendSysMessage($"生成回复失败：{ex.Message}");
            }
            finally
            {
                await ShowHint("");
                await OnUIAsync(() =>
                {
                    UserQuestion = "";
                    IsTraking = false;
                });
            }
        }

        #endregion
    }

}
