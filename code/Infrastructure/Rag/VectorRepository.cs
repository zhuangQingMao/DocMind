using Microsoft.Data.Sqlite;

namespace DocMind
{
    public class VectorRepository : IVectorRepository
    {
        private readonly string _connectionString;
        private const string TableName = "DocumentVectors";

        private VectorRepository()
        {
            _connectionString = @"Data Source=D:\code\rag_data.db";
        }

        public static async Task<VectorRepository> CreateAsync()
        {
            var instance = new VectorRepository();

            await instance.InitializeAsync();

            return instance;
        }

        private async Task InitializeAsync()
        {
            await CreateTableAsync();

            await ClearTableAsync();
        }

        private async Task CreateTableAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {TableName} (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FileName TEXT NOT NULL,
                    ChunkIndex INT NOT NULL,
                    OriginalText TEXT NOT NULL,
                    Vector BLOB NOT NULL);";

            await command.ExecuteNonQueryAsync();
        }

        private async Task ClearTableAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {TableName};";

            await command.ExecuteNonQueryAsync();
        }

        public async Task SaveVectorAsync(string fileName, int chunkIndex, string text, float[] vector)
        {
            var vectorBytes = FloatArrayToByteArray(vector);
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $@"INSERT INTO {TableName} (FileName, ChunkIndex, OriginalText, Vector) 
                                     VALUES (@fileName, @chunkIndex, @text, @vector);";

            command.Parameters.AddWithValue("@fileName", fileName);
            command.Parameters.AddWithValue("@chunkIndex", chunkIndex);
            command.Parameters.AddWithValue("@text", text);
            command.Parameters.AddWithValue("@vector", vectorBytes);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<ChunkSortResult>> FindRelevantChunks(float[] queryVector, int topK)
        {
            var allRecords = new List<ChunkRecord>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT OriginalText, Vector,ChunkIndex FROM {TableName};";

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    byte[] vectorBytes = reader.GetFieldValue<byte[]>(1);
                    float[] vector = ByteArrayToFloatArray(vectorBytes);

                    allRecords.Add(new ChunkRecord
                    {
                        OriginalText = reader.GetString(0),
                        ChunkIndex = Convert.ToInt32(reader.GetString(2)),
                        Vector = vector
                    });
                }
            }

            var resultsWithScores = allRecords
                .Select(record => new ChunkSortResult
                {
                    Record = record,
                    Score = CosineSimilarity(queryVector, record.Vector),
                    PageNumber = record.ChunkIndex,
                })
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();

            return resultsWithScores;
        }

        private static byte[] FloatArrayToByteArray(float[] floats)
        {
            var byteArray = new byte[floats.Length * sizeof(float)];
            Buffer.BlockCopy(floats, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        private static float[] ByteArrayToFloatArray(byte[] bytes)
        {
            var floatArray = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, floatArray, 0, bytes.Length);
            return floatArray;
        }

        private static float CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Vectors must have the same length.");

            if (vectorA.Any(f => float.IsNaN(f) || float.IsInfinity(f)) || vectorB.Any(f => float.IsNaN(f) || float.IsInfinity(f)))
                throw new ArgumentException("Input vectors contain NaN or Infinity values, which will invalidate the cosine similarity calculation.");

            float res = 0;

            float dotProduct = 0;
            float magnitudeA = 0;
            float magnitudeB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            if (magnitudeA == 0 || magnitudeB == 0)
                return res;

            res = (float)(dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB)));
            return res;
        }
    }
}
