using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TechScriptAid.Core.DTOs.AI;
using TechScriptAid.Core.Interfaces.AI;
using Microsoft.AspNetCore.Mvc.ApiExplorer; // Add this namespace


namespace TechScriptAid.API.Controllers
{
    /// <summary>
    /// Administrative controller for AI operations
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/ai/admin")]
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin")]
    public class AIAdminController : ControllerBase
    {
        private readonly IAIConfigurationService _configurationService;
        private readonly IAIOperationLogger _operationLogger;
        private readonly ILogger<AIAdminController> _logger;

        public AIAdminController(
            IAIConfigurationService configurationService,
            IAIOperationLogger operationLogger,
            ILogger<AIAdminController> logger)
        {
            _configurationService = configurationService;
            _operationLogger = operationLogger;
            _logger = logger;
        }

        /// <summary>
        /// Gets the current AI configuration
        /// </summary>
        /// <returns>AI configuration</returns>
        [HttpGet("configuration")]
        [ProducesResponseType(typeof(AIConfiguration), StatusCodes.Status200OK)]
        public async Task<ActionResult<AIConfiguration>> GetConfiguration()
        {
            var config = await _configurationService.GetConfigurationAsync();

            // Mask sensitive information
            config.ApiKey = "***" + config.ApiKey.Substring(Math.Max(0, config.ApiKey.Length - 4));

            return Ok(config);
        }

        /// <summary>
        /// Updates the AI configuration
        /// </summary>
        /// <param name="configuration">New configuration</param>
        /// <returns>Success status</returns>
        [HttpPut("configuration")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateConfiguration(
            [FromBody] AIConfiguration configuration)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _configurationService.UpdateConfigurationAsync(configuration);
                _logger.LogInformation("AI configuration updated by {User}", User.Identity?.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AI configuration");
                return BadRequest(new ProblemDetails
                {
                    Title = "Configuration Update Failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }

        /// <summary>
        /// Validates the current AI configuration
        /// </summary>
        /// <returns>Validation result</returns>
        [HttpPost("configuration/validate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateConfiguration()
        {
            var isValid = await _configurationService.ValidateConfigurationAsync();

            if (isValid)
            {
                return Ok(new { valid = true, message = "Configuration is valid" });
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Configuration Invalid",
                Detail = "The current AI configuration is invalid or cannot connect to the service",
                Status = StatusCodes.Status400BadRequest
            });
        }

        /// <summary>
        /// Gets AI operation history
        /// </summary>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="startDate">Start date filter</param>
        /// <param name="endDate">End date filter</param>
        /// <returns>Operation history</returns>
        [HttpGet("operations")]
        [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOperationHistory(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Pagination",
                    Detail = "Page number must be >= 1 and page size must be between 1 and 100",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var operations = await _operationLogger.GetOperationHistoryAsync(
                pageNumber, pageSize, startDate, endDate);

            return Ok(operations);
        }

        /// <summary>
        /// Gets AI usage statistics
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Usage statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(AIUsageStatistics), StatusCodes.Status200OK)]
        public async Task<ActionResult<AIUsageStatistics>> GetUsageStatistics(
            [FromQuery, Required] DateTime startDate,
            [FromQuery, Required] DateTime endDate)
        {
            if (endDate < startDate)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Date Range",
                    Detail = "End date must be after start date",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var statistics = await _operationLogger.GetUsageStatisticsAsync(startDate, endDate);

            return Ok(statistics);
        }
    }
}
