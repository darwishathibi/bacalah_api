using bacalah.API.Models;

namespace bacalah.API.Services;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(SearchRequestDto search);
}