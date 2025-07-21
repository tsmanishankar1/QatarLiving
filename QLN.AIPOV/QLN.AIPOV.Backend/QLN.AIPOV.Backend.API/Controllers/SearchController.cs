using Microsoft.AspNetCore.Mvc;
using QLN.AIPOV.Backend.Application.Interfaces;

namespace QLN.AIPOV.Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController(ISearchService searchService) : ControllerBase
    {
        [HttpGet("keyword")]
        public async Task<IActionResult> KeywordSearch([FromQuery] string query, [FromQuery] int top = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty");

            var results = await searchService.KeywordSearchAsync(query, top);
            return Ok(results);
        }

        [HttpGet("vector")]
        public async Task<IActionResult> VectorSearch([FromQuery] string query, [FromQuery] int top = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty");

            var results = await searchService.VectorSearchAsync(query, top);
            return Ok(results);
        }

        [HttpGet("hybrid")]
        public async Task<IActionResult> HybridSearch([FromQuery] string query, [FromQuery] int top = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty");

            var results = await searchService.HybridSearchAsync(query, top);
            return Ok(results);
        }
    }
}