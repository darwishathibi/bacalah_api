using bacalah.API.Models;
using bacalah.Entities.Data;
using bacalah.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace bacalah.API.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _dbContext;
    
    public SearchService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SearchResultDto> SearchAsync(SearchRequestDto search)
    {
        var query = _dbContext.Documents
            .Include(d => d.Category)
            .Include(d => d.DocumentTags)
            .ThenInclude(dt => dt.Tag)
            .Include(d => d.User)
            .AsQueryable();

        // search by query
        if (string.IsNullOrWhiteSpace(search.Query))
        {
            var searchTerm = search.Query.Trim().ToLower();
            query = query.Where(d => 
                d.Title.ToLower().Contains(searchTerm) ||
                d.Content.ToLower().Contains(searchTerm));
        }
        
        // filter by category
        if (search.CategoryId.HasValue)
        {
            query = query.Where(d => d.CategoryId == search.CategoryId.Value);
        }
        
        // filter by tags
        if (search.TagIds.Any())
        {
            query = query.Where(d => d.DocumentTags.Any(dt => search.TagIds.Contains(dt.TagId)));
        }
        
        // sorting
        query = ApplySorting(query, search.SortBy, search.SortDescending);
        
        var total = await query.CountAsync();
        
        // pagination
        var documents = await query
            .Skip((search.PageNumber - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync();

        var docs = documents.Select(d => new DocumentListDto()
        {
            Id = d.Id,
            Title = d.Title,
            ContentPreview = d.Content.Length > 150 
                ? d.Content.Substring(0, 150) + "..." 
                : d.Content,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            UserName = d.User.UserName ?? d.User.Email,
            CategoryId = d.CategoryId,
            CategoryName = d.Category?.Name,
            TagNames = d.DocumentTags.Select(dt => dt.Tag.Name).ToList()
        }).ToList();

        return new SearchResultDto
        {
            Documents = docs,
            TotalCount = total,
            PageNumber = search.PageNumber,
            TotalPages = (int)Math.Ceiling(total / (double)search.PageSize)
        };
    }
    
    private IQueryable<Document> ApplySorting(IQueryable<Document> query, string? sortBy, bool sortDescending)
    {
        return (sortBy?.ToLower(), sortDescending) switch
        {
            ("title", false) => query.OrderBy(d => d.Title),
            ("title", true) => query.OrderByDescending(d => d.Title),
            ("createdat", false) => query.OrderBy(d => d.CreatedAt),
            ("createdat", true) => query.OrderByDescending(d => d.CreatedAt),
            _ => query.OrderByDescending(d => d.UpdatedAt) // Default sort
        };
    }
}