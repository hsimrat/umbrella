using Microsoft.AspNetCore.Mvc;
using TechScriptAid.API.DTOs;
using TechScriptAid.API.Services;



namespace TechScriptAid.API.Controllers
{
    /// <summary>
    /// Manages document operations including CRUD and AI analysis
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// Get all documents with optional filtering
        /// </summary>
        /// <param name="category">Filter by category</param>
        /// <param name="status">Filter by status</param>
        /// <returns>List of documents</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocuments(
            [FromQuery] string? category = null,
            [FromQuery] DocumentStatus? status = null)
        {
            _logger.LogInformation("Getting documents with filters: Category={Category}, Status={Status}",
                category, status);

            var documents = await _documentService.GetDocumentsAsync(category, status);
            return Ok(documents);
        }

        /// <summary>
        /// Get a specific document by ID
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>The requested document</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DocumentDto>> GetDocument(Guid id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return NotFound(new { message = $"Document with ID {id} not found" });
            }

            return Ok(document);
        }

        /// <summary>
        /// Create a new document
        /// </summary>
        /// <param name="createDto">Document creation data</param>
        /// <returns>The created document</returns>
        [HttpPost]
        [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DocumentDto>> CreateDocument([FromBody] CreateDocumentDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating new document with title: {Title}", createDto.Title);

            var document = await _documentService.CreateDocumentAsync(createDto);

            return CreatedAtAction(
                nameof(GetDocument),
                new { id = document.Id },
                document);
        }

        /// <summary>
        /// Update an existing document
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <param name="updateDto">Updated document data</param>
        /// <returns>No content on success</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] UpdateDocumentDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _documentService.UpdateDocumentAsync(id, updateDto);
            if (!result)
            {
                return NotFound(new { message = $"Document with ID {id} not found" });
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a document (soft delete)
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            var result = await _documentService.DeleteDocumentAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"Document with ID {id} not found" });
            }

            _logger.LogInformation("Document {DocumentId} deleted successfully", id);
            return NoContent();
        }

        /// <summary>
        /// Search documents by keyword
        /// </summary>
        /// <param name="searchTerm">Search keyword</param>
        /// <returns>List of matching documents</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<DocumentDto>), StatusCodes.Status200OK)]
        [ResponseCache(Duration = 30)]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> SearchDocuments(
            [FromQuery] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new { message = "Search term cannot be empty" });
            }

            var documents = await _documentService.SearchDocumentsAsync(searchTerm);
            return Ok(documents);
        }

        /// <summary>
        /// Analyze a document using AI
        /// </summary>
        /// <param name="id">Document ID</param>
        /// <returns>Analysis results</returns>
        [HttpPost("{id}/analyze")]
        [ProducesResponseType(typeof(DocumentAnalysisDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<DocumentAnalysisDto>> AnalyzeDocument(Guid id)
        {
            try
            {
                var analysis = await _documentService.AnalyzeDocumentAsync(id);
                if (analysis == null)
                {
                    return NotFound(new { message = $"Document with ID {id} not found" });
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document {DocumentId}", id);
                return StatusCode(503, new { message = "AI service temporarily unavailable" });
            }
        }
    }
}
