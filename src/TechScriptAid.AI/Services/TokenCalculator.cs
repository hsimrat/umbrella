using TechScriptAid.Core.Interfaces.AI;

namespace TechScriptAid.AI.Services
{
    /// <summary>
    /// Calculates tokens and costs for AI operations
    /// </summary>
    public class TokenCalculator : ITokenCalculator
    {

        private const double CharactersPerToken = 4.0;
        private const decimal CostPer1kTokensGPT35 = 0.0015m;
        private const decimal CostPer1kTokensGPT4 = 0.03m;

        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            return (int)Math.Ceiling(text.Length / CharactersPerToken);
        }

        public bool IsWithinTokenLimit(string text, int maxTokens)
        {
            return EstimateTokens(text) <= maxTokens;
        }

        public decimal CalculateCost(int promptTokens, int completionTokens, string model)
        {
            var totalTokens = promptTokens + completionTokens;
            var costPer1kTokens = model.Contains("gpt-4") ? CostPer1kTokensGPT4 : CostPer1kTokensGPT35;

            return (totalTokens / 1000.0m) * costPer1kTokens;
        }


        //    private readonly Dictionary<string, Tokenizer> _tokenizers;
        //    private readonly Dictionary<string, (decimal promptCost, decimal completionCost)> _modelPricing;

        //    public TokenCalculator()
        //    {
        //        _tokenizers = new Dictionary<string, Tokenizer>();

        //        // Initialize model pricing (per 1K tokens)
        //        _modelPricing = new Dictionary<string, (decimal, decimal)>
        //        {
        //            ["gpt-4"] = (0.03m, 0.06m),
        //            ["gpt-4-32k"] = (0.06m, 0.12m),
        //            ["gpt-4-turbo"] = (0.01m, 0.03m),
        //            ["gpt-3.5-turbo"] = (0.0005m, 0.0015m),
        //            ["text-embedding-ada-002"] = (0.0001m, 0m),
        //            ["text-embedding-3-small"] = (0.00002m, 0m),
        //            ["text-embedding-3-large"] = (0.00013m, 0m)
        //        };
        //    }

        //    public int EstimateTokens(string text)
        //    {
        //        // Simple estimation: ~4 characters per token for English
        //        // For production, use proper tokenizer like tiktoken
        //        return (int)Math.Ceiling(text.Length / 4.0);
        //    }

        //    public decimal CalculateCost(int promptTokens, int completionTokens, string model)
        //    {
        //        if (!_modelPricing.ContainsKey(model))
        //        {
        //            // Default pricing if model not found
        //            return (promptTokens * 0.01m + completionTokens * 0.03m) / 1000;
        //        }

        //        var (promptCost, completionCost) = _modelPricing[model];
        //        return (promptTokens * promptCost + completionTokens * completionCost) / 1000;
        //    }

        //    public bool IsWithinTokenLimit(string text, int maxTokens)
        //    {
        //        var estimatedTokens = EstimateTokens(text);
        //        return estimatedTokens <= maxTokens;
        //    }
    }
}
