using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;


namespace TechScriptAid.API.Controllers
{
    /// <summary>
    /// Controller for AI operations
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/ai")]
    [ApiVersion("1.0")]
   // [Authorize]
    [EnableRateLimiting("ai-policy")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(
            IAIService aiService,
            ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Summarizes the provided text
        /// </summary>
        /// <param name="request">Summarization request</param>
        /// <returns>Summarization response</returns>
        [HttpPost("summarize")]
        [ProducesResponseType(typeof(SummarizationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SummarizationResponse>> Summarize(
            [FromBody] SummarizationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.UserId = User.Identity?.Name;

                var response = await _aiService.SummarizeAsync(request);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in summarization");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in summarization");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = ex.Message,
                    Status = StatusCodes.Status500InternalServerError
                });

            }
        }

        /// <summary>
        /// Generates content based on the provided prompt
        /// </summary>
        /// <param name="request">Content generation request</param>
        /// <returns>Generated content response</returns>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(ContentGenerationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ContentGenerationResponse>> GenerateContent(
            [FromBody] ContentGenerationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.UserId = User.Identity?.Name;

                var response = await _aiService.GenerateContentAsync(request);

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in content generation");
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in content generation");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Analyzes text for various insights
        /// </summary>
        /// <param name="request">Text analysis request</param>
        /// <returns>Analysis response</returns>
        [HttpPost("analyze")]
        [ProducesResponseType(typeof(TextAnalysisResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TextAnalysisResponse>> AnalyzeText(
            [FromBody] TextAnalysisRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.UserId = User.Identity?.Name;

                var response = await _aiService.AnalyzeTextAsync(request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in text analysis");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Generates embeddings for the provided texts
        /// </summary>
        /// <param name="request">Embedding request</param>
        /// <returns>Embedding response</returns>
        [HttpPost("embeddings")]
        [ProducesResponseType(typeof(EmbeddingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmbeddingResponse>> GenerateEmbeddings(
            [FromBody] EmbeddingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (request.Texts.Length == 0)
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = "At least one text must be provided",
                        Status = StatusCodes.Status400BadRequest
                    });

                if (request.Texts.Length > 100)
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = "Maximum 100 texts can be processed in a single request",
                        Status = StatusCodes.Status400BadRequest
                    });

                request.UserId = User.Identity?.Name;

                var response = await _aiService.GenerateEmbeddingAsync(request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Performs semantic search on documents
        /// </summary>
        /// <param name="request">Semantic search request</param>
        /// <returns>Search results</returns>
        [HttpPost("search")]
        [ProducesResponseType(typeof(SemanticSearchResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SemanticSearchResponse>> SemanticSearch(
            [FromBody] SemanticSearchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.UserId = User.Identity?.Name;

                var response = await _aiService.SemanticSearchAsync(request);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in semantic search");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing your request",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }
    }
}
