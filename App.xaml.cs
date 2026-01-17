using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace DocMind
{
    public partial class App : Application
    {
        public required IServiceProvider ServiceProvider { get; set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();

            await ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

            mainWindow.Show();
        }

        private static async Task ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            services.AddSingleton<ChatViewModel>();
            services.AddSingleton<IRagCoreService, RagCoreService>();
            services.AddSingleton<IEmbeddingService, EmbeddingService>();
            services.AddSingleton<IVectorRepository>(await VectorRepository.CreateAsync());
            services.AddSingleton<IFileManagerFactory, FileManagerFactory>();
            services.AddSingleton<IDocumentLoaderService, DocumentLoaderService>();
            services.AddSingleton<IDocumentChunker, DocumentChunker>();
            services.AddSingleton<ILLMChatService, LLMChatService>();
        }
    }
}
