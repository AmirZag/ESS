
namespace ESS.Api.DTOs.Common;

public sealed class CollectionResponse<T> : ICollectionResponse<T>, ILinkResponse
{
    public List<T> Items { get; init ; }
    public List<LinkDto> Links { get ; set ; }
}
