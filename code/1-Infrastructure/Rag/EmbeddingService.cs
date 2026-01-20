using LLama;
using LLama.Common;
using System.Windows;

namespace DocMind
{
    public interface IEmbeddingService : IDisposable
    {
        Task<float[]> GetVectorAsync(string text);
    }

    public class EmbeddingService : IEmbeddingService
    {
        private LLamaWeights _model;
        private LLamaEmbedder _embedder;
        private string _modelPath;
        private ModelParams _modelParams;
        private bool _disposed = false;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public EmbeddingService()
        {
            try
            {
                _modelPath = @"D:\code\gte-qwen2-1.5b-instruct-Q5_K_M.gguf";
                _modelParams = new ModelParams(_modelPath)
                {
                    ContextSize = 32768,
                    GpuLayerCount = 50,
                    MainGpu = 0,
                    Embeddings = true,
                    SplitMode = LLama.Native.GPUSplitMode.None,
                    UseMemoryLock = false,
                    UseMemorymap = true,
                    FlashAttention = true,
                    BatchSize = 1024,
                    UBatchSize = 1024,
                    Threads = 8,
                    PoolingType = LLama.Native.LLamaPoolingType.Mean
                };

                _model = LLamaWeights.LoadFromFile(_modelParams);
                _embedder = new LLamaEmbedder(_model, _modelParams);
            }

            catch (Exception ex)
            {
                MessageBox.Show($"EmbeddingService初始化失败: {ex}");
                throw;
            }
        }

        public async Task<float[]> GetVectorAsync(string text)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EmbeddingService));
            }

            await _lock.WaitAsync();

            try
            {
                return (await _embedder.GetEmbeddings(text))[0];
            }
            catch (Exception ex)
            {
                MessageBox.Show($"GetVectorAsync执行失败: {ex}");
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _embedder?.Dispose();
                _model?.Dispose();
                _disposed = true;
            }
        }
    }
}
