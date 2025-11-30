using bacalah.API.Models;
using bacalah.Entities.Data;
using bacalah.Entities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace bacalah.API.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _dbContext;
    
    public DocumentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<DocumentListDto>> GetDocumentsAsync( int pageNumber = 1, int pageSize = 10)
    {
        var query = _dbContext.Documents
            .Include(d => d.Category)
            .Include(d => d.DocumentTags)
            .ThenInclude(dt => dt.Tag)
            .Include(d => d.User)
            .OrderByDescending(d => d.UpdatedAt);
        
        var totalCount = await query.CountAsync();
        var documents = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PaginatedResult<DocumentListDto>
        {
            Items = documents.Select(MapToDocumentListDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<DocumentDto?> GetByIdAsync(int id)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.Category)
            .Include(d => d.DocumentTags)
            .ThenInclude(dt => dt.Tag)
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);
        
        if (doc == null)
            return null;

        return new DocumentDto
        {
            Id = doc.Id,
            Title = doc.Title,
            Content = doc.Content,
            CreatedAt = doc.CreatedAt,
            UpdatedAt = doc.UpdatedAt,
            UserId = doc.UserId,
            UserName = doc.User.UserName ?? doc.User.Email,
            CategoryId = doc.CategoryId,
            CategoryName = doc.Category?.Name,
            Tags = doc.DocumentTags.Select(dt => dt.Tag.Name).ToList()
        };
    }

    public async Task<PaginatedResult<DocumentListDto>> GetByCategoryAsync(int? categoryId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _dbContext.Documents
            .Where(d => d.CategoryId == categoryId)
            .Include(d => d.Category)
            .Include(d => d.DocumentTags)
            .ThenInclude(d => d.Tag)
            .Include(d => d.User)
            .OrderByDescending(d => d.UpdatedAt);
        
        var totalCount = await query.CountAsync();
        var docs = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<DocumentListDto>
        {
            Items = docs.Select(MapToDocumentListDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<DocumentDto> CreateAsync(CreateDocumentDto createDocumentDto, string userId)
    {
        if (createDocumentDto.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AnyAsync(c => c.Id == createDocumentDto.CategoryId.Value);
                
            if (!categoryExists)
            {
                throw new ArgumentException("Category does not exist.");
            }
        }

        var doc = new Document
        {
            Title = createDocumentDto.Title,
            Content = createDocumentDto.Content,
            UserId = userId,
            CategoryId = createDocumentDto.CategoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Documents.Add(doc);
        await _dbContext.SaveChangesAsync();
        
        await ProcessTagsAsync(doc, createDocumentDto.Tags);
        return await GetByIdAsync(doc.Id) ?? throw new Exception("Failed to create document");
    }

    public async Task<DocumentDto> UpdateAsync(int id, UpdateDocumentDto updateDocumentDto)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.DocumentTags)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null)
        {
            throw new ArgumentException("Document does not exist.");
        }

        if (updateDocumentDto.CategoryId.HasValue)
        {
            var categoryExists = await _dbContext.Categories
                .AnyAsync(c => c.Id == updateDocumentDto.CategoryId.Value);
                
            if (!categoryExists)
            {
                throw new ArgumentException("Category does not exist.");
            }
        }
        
        doc.Title = updateDocumentDto.Title;
        doc.Content = updateDocumentDto.Content;
        doc.CategoryId = updateDocumentDto.CategoryId;
        doc.UpdatedAt = DateTime.UtcNow;
        
        await ProcessTagsAsync(doc, updateDocumentDto.Tags);

        await _dbContext.SaveChangesAsync();

        return await GetByIdAsync(doc.Id) ?? throw new Exception("Failed to update document");
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
       var doc = await _dbContext.Documents
           .FirstOrDefaultAsync(d => d.Id == id);
       
         if (doc == null)
             return false;
         
         _dbContext.Documents.Remove(doc);
         await _dbContext.SaveChangesAsync();
            
         return true;
    }

    public async Task<List<DocumentListDto>> GetRecentAsync( int count = 5)
    {
        var documents = await _dbContext.Documents
            .Include(d => d.Category)
            .Include(d => d.DocumentTags)
            .ThenInclude(dt => dt.Tag)
            .Include(d => d.User)
            .OrderByDescending(d => d.UpdatedAt)
            .Take(count)
            .ToListAsync();

        return documents.Select(MapToDocumentListDto).ToList();
    }

    private DocumentListDto MapToDocumentListDto(Document doc)
    {
        var contentPreview = doc.Content.Length > 150 
            ? doc.Content.Substring(0, 150) + "..." 
            : doc.Content; 
        
        return new DocumentListDto
        {
            Id = doc.Id,
            Title = doc.Title,
            ContentPreview = contentPreview,
            CreatedAt = doc.CreatedAt,
            UpdatedAt = doc.UpdatedAt,
            UserName = doc.User.UserName ?? doc.User.Email,
            CategoryId = doc.CategoryId,
            CategoryName = doc.Category?.Name,
            TagNames = doc.DocumentTags.Select(dt => dt.Tag.Name).ToList()
        };
    }

    private async Task ProcessTagsAsync(Document doc, List<string> tagNames)
    {
        var existingDocTags = _dbContext.DocumentTags
            .Where(dt => dt.DocumentId == doc.Id);
        _dbContext.DocumentTags.RemoveRange(existingDocTags);
        
        foreach (var tagName in tagNames.Distinct())
        {
            var tag = await GetOrCreateTagAsync(tagName.Trim());
                
            var documentTag = new DocumentTag
            {
                DocumentId = doc.Id,
                TagId = tag.Id,
                CreatedAt = DateTime.UtcNow
            };
                
            _dbContext.DocumentTags.Add(documentTag);
        }
    }
    
    private async Task<Tag> GetOrCreateTagAsync(string tagName)
    {
        var tag = await _dbContext.Tags
            .FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());

        if (tag == null)
        {
            tag = new Tag
            {
                Name = tagName,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();
        }

        return tag;
    }
}