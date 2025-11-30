using System.Security.Claims;
using bacalah.API.Models;
using bacalah.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bacalah.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    
    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }
    
    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SearchResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<List<SearchResultDto>>), 500)]
    public async Task<ActionResult<List<SearchResultDto>>> Search(
        [FromQuery] string? query,
        [FromQuery] int? categoryId,
        [FromQuery] string? tagIds, // Comma-separated tag IDs
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "UpdatedAt",
        [FromQuery] bool sortDescending = true)
    {
        try
        {
            var userId = GetUserId();
            var search = new SearchRequestDto
            {
                Query = query,
                CategoryId = categoryId,
                TagIds = ParseTagIds(tagIds),
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending
            };
            
            var res = await _searchService.SearchAsync(search);

            return Ok(new ApiResponseDto<SearchResultDto>
            {
                Success = true,
                Data = res
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ApiResponseDto<SearchResultDto>
            {
                Success = false,
                Message = "An error occurred while searching documents.",
                Errors = new List<string> { e.Message }
            });
        }
    }
    
    private List<int> ParseTagIds(string? tagIds)
    {
        if (string.IsNullOrWhiteSpace(tagIds))
            return new List<int>();

        return tagIds.Split(',')
            .Where(id => int.TryParse(id.Trim(), out _))
            .Select(id => int.Parse(id.Trim()))
            .ToList();
    }
}