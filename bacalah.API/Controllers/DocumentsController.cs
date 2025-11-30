using System.Security.Claims;
using bacalah.API.Models;
using bacalah.API.Services;
using bacalah.Entities.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace bacalah.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    
    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }
    
    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
               ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentListDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<List<DocumentListDto>>), 500)]
    public async Task<ActionResult<ApiResponseDto<PaginatedResult<DocumentListDto>>>> GetDocuments([FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var res = await _documentService.GetDocumentsAsync( pageNum, pageSize);

            return Ok(new ApiResponseDto<PaginatedResult<DocumentListDto>>
            {
                Success = true,
                Data = res
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ApiResponseDto<PaginatedResult<DocumentListDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving documents.",
                Errors = new List<string> { e.Message }
            });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(List<DocumentDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<List<DocumentDto>>), 500)]
    public async Task<ActionResult<ApiResponseDto<DocumentDto?>>> GetById(int id)
    {
        try
        {
            var doc = await _documentService.GetByIdAsync(id);

            if (doc == null)
            {
                return NotFound(new ApiResponseDto<DocumentDto>
                {
                    Success = false,
                    Message = "Document not found."
                });
            }
            
            return Ok(new ApiResponseDto<DocumentDto>
            {
                Success = true,
                Data = doc
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ApiResponseDto<DocumentDto>
            {
                Success = false,
                Message = "An error occurred while retrieving the document.",
                Errors = new List<string> { e.Message }
            });
        }
    }

    [HttpGet("category/{categoryId}")]
    [ProducesResponseType(typeof(List<DocumentListDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<List<DocumentListDto>>), 500)]
    public async Task<ActionResult<ApiResponseDto<PaginatedResult<DocumentListDto>>>> GetByCategory(int? categoryId,
        [FromQuery] int pageNum = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var res = await _documentService.GetByCategoryAsync(categoryId, pageNum, pageSize);

            return Ok(new ApiResponseDto<PaginatedResult<DocumentListDto>>
            {
                Success = true,
                Data = res
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ApiResponseDto<PaginatedResult<DocumentListDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving documents by category.",
                Errors = new List<string> { e.Message }
            });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<DocumentDto>), 500)]
    public async Task<ActionResult<ApiResponseDto<DocumentDto>>> Create([FromBody] CreateDocumentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto<DocumentDto>
                {
                    Success = false,
                    Message = "Invalid input data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            
            var userId = GetUserId();
            var doc = await _documentService.CreateAsync(dto, userId);
            
            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, new ApiResponseDto<DocumentDto>
            {
                Success = true,
                Message = "Document created successfully.",
                Data = doc
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ApiResponseDto<DocumentDto>
            {
                Success = false,
                Message = "An error occurred while creating the document.",
                Errors = new List<string> { e.Message }
            });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DocumentDto), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<DocumentDto>), 500)]
    public async Task<ActionResult<ApiResponseDto<DocumentDto>>> Update(int id, [FromBody] UpdateDocumentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto<DocumentDto>
                {
                    Success = false,
                    Message = "Invalid input data.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }
            
            var doc = await _documentService.UpdateAsync(id, dto);

            return Ok(new ApiResponseDto<DocumentDto>
            {
                Success = true,
                Message = "Document updated successfully.",
                Data = doc
            });
        }
        catch (ArgumentException e)
        {
            return BadRequest(new ApiResponseDto<DocumentDto>
            {
                Success = false,
                Message = e.Message,
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ApiResponseDto<DocumentDto>
            {
                Success = false,
                Message = "An error occurred while updating the document.",
                Errors = new List<string> { e.Message }
            });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(bool), 204)]
    [ProducesResponseType(typeof(ApiResponseDto<bool>), 500)]
    public async Task<ActionResult<ApiResponseDto>> Delete(int id)
    {
        try
        {
            var deleted = await _documentService.DeleteAsync(id);

            if (!deleted)
            {
                return NotFound(new ApiResponseDto
                {
                    Success = false,
                    Message = "Document not found."
                });
            }

            return Ok(new ApiResponseDto
            {
                Success = true,
                Message = "Document deleted successfully.",
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ApiResponseDto
            {
                Success = false,
                Message = "An error occurred while deleting the document.",
                Errors = new List<string> { e.Message }
            });
        }
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<DocumentListDto>), 200)]
    [ProducesResponseType(typeof(ApiResponseDto<List<DocumentListDto>>), 500)]
    public async Task<ActionResult<List<DocumentListDto>>> GetRecent([FromQuery] int count = 5)
    {
        try
        {
            var doc = await _documentService.GetRecentAsync(count);

            return Ok(new ApiResponseDto<List<DocumentListDto>>
            {
                Success = true,
                Data = doc
            });
        }
        catch (Exception e)
        {
            return StatusCode(500, new ApiResponseDto<List<DocumentListDto>>
            {
                Success = false,
                Message = "An error occurred while retrieving recent documents.",
                Errors = new List<string> { e.Message }
            });
        }
    }
}