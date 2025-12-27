using DocMind.code.Infrastructure;

namespace DocMind
{
    public class RagCoreService(
        IVectorRepository repository,
        IEmbeddingService embeddingService,
        IDocumentChunker documentChunker,
        ILLMChatService lLMChatService ) : IRagCoreService
    {
        private readonly Dictionary<FileType, string> _prompDict = new()
        {
            { FileType.Txt, @"# RAG 助手角色定义
你是一个严谨、专业的 RAG（检索增强生成）知识问答助手。
## 任务约束：
1.  **信息来源限定（核心）：** 严格根据 [上下文] 中提供的资料来回答用户的问题，不得使用外部知识。
2.  **无答案处理：** 如果在 [上下文] 中找不到问题的确切答案，你必须使用类似“根据提供的资料，我无法找到这个问题的答案。” 或 “资料中未提及相关信息。” 等明确的措辞表示不知道，**禁止捏造或推测事实**。
3.  **引用规范：** 在回答中，尽可能引用上下文中的关键词或短语来支持你的答案，以增加可信度。
4.  **回复格式：** 优先以简洁、清晰的条理化文本（如分点、分段）进行回复，保持专业的语气。"
            },

            { FileType.Word, @"# RAG 助手角色定义
你是一个专业的、值得信赖的文档问答系统。你的唯一任务是根据提供的[上下文]精确回答[用户查询]。
## 输出结构要求：
1. **回答主体格式：** 给出专业的回答主体，如果包含多个答案或要点，请使用序号 1.、2.、3. 等进行分点阐述。
2. **页码引用规范：** 回答的主体部分不得出现页码。在回答主体结束后，在单独一行使用 **【引用页码汇总】** 作为前缀，列出所有实际引用到的**不重复**页码（页码间用逗号和空格分隔）。
3. **无答案处理：** 当资料中找不到答案时，直接回答“找不到相关信息”，并且不需要输出最后一行的【引用页码汇总】。"
            }
        };

        public async Task ImportDocumentAsync(string fileName, object fileMeta)
        {
            var chunks = documentChunker.ChunkAndGenerateMetadata(fileName, fileMeta);

            foreach (var chunk in chunks)
            {
                var vector = await embeddingService.GetVectorAsync(chunk.Text);

                await repository.SaveVectorAsync(fileName, chunk.ChunkIndex, chunk.Text, vector);
            }
        }

        public async Task<string> GetOriginContext(string userQuestion, FileType type)
        {
            var queryVector = await embeddingService.GetVectorAsync(userQuestion);
            var relevantText = await repository.FindRelevantChunks(queryVector, 10);

            return SpliceContext(relevantText, type);
        }

        private string SpliceContext(List<ChunkSortResult> results, FileType type)
        {
            if (results == null || results.Count == 0)
                return "无相关信息";

            const string CHUNK_SEPARATOR = "\n\n### CHUNK ###\n\n";

            if (type == FileType.Txt)
                return string.Join(CHUNK_SEPARATOR, results.Select(a => a.Record.OriginalText));

            if (type == FileType.Word)
                return string.Join(CHUNK_SEPARATOR, results.Select(a => $"页码：{a.PageNumber}，内容：{a.Record.OriginalText}"));

            return "";
        }

        public async IAsyncEnumerable<string> firstChat_Stream(string userQuestion, string context, FileType type)
        {
            var systemPrompt = GetSystemPrompt(type);
            var userPrompt = $@"## 上下文资料
### CONTEXT START ###
{context}
### CONTEXT END ###

请严格根据上面的 [上下文资料]，回答以下用户的问题：
### 用户问题：
{userQuestion}";

            await foreach (var chunk in lLMChatService.StreamChatAsync(userPrompt, systemPrompt))
            {
                yield return chunk;
            }
        }

        public async Task<string> secondChat(string context, string firstAnswer, FileType type)
        {
            var systemPrompt = GetSystemPrompt(type);
            var userPrompt = $@"# 任务：精确溯源与原句提取

你是一个严谨的**原句提取分析器**。你的唯一任务是根据提供的 [最终答案] 和 [原始上下文]，**精确提取**出所有直接支持 [最终答案] 的原始句子。

## 提取约束：
1. **只提取原句：** 提取出的内容必须是 [原始上下文] 中存在的完整句子，禁止修改、总结或推测。
2. **输出格式：** 提取出的每个句子之间必须使用分隔符 `|||` 进行分隔。
3. **禁止解释：** 除了提取出的原句和分隔符，**禁止添加任何额外的文字或解释**。

## 输入数据：
### 最终答案：
{firstAnswer}
### CONTEXT START ###
{context}
### CONTEXT END ###";

            return await lLMChatService.ChatAsync(userPrompt, systemPrompt);
        }

        private string GetSystemPrompt(FileType type)
        {
            if (_prompDict.TryGetValue(type, out string? value) && value != null)
                return value;

            throw new Exception("获取不到Prompt");
        }
    }
}
