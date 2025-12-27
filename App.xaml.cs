using CommunityToolkit.Mvvm.Messaging;
using DocMind.code.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DocMind
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 定义 DI 容器
        public IServiceProvider ServiceProvider { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            await ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private async Task ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            services.AddSingleton<ChatViewModel>();

            services.AddSingleton<IRagCoreService, RagCoreService>();
            services.AddSingleton<IEmbeddingService, EmbeddingService>();
            var repository = await VectorRepository.CreateAsync();
            services.AddSingleton<IVectorRepository>(repository);

            services.AddSingleton<IFileManagerFactory, FileManagerFactory>();
            services.AddSingleton<IDocumentLoaderService, DocumentLoaderService>();

            services.AddSingleton<IDocumentChunker, DocumentChunker>();

            services.AddSingleton<ILLMChatService, LLMChatService>();

            


        }
    }
}
