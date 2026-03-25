using Microsoft.AspNetCore.Mvc;

namespace RagClient.Api.Models;

/// <summary>
/// Query parameters for GET /api/collections, forwarded to the upstream /v1/collection endpoint.
/// </summary>
public class GetCollectionsQuery
{
    public int? Id { get; set; }
    public string? Name { get; set; }

    [FromQuery(Name = "name_eq")]
    public string? NameEq { get; set; }

    public string? Key { get; set; }

    [FromQuery(Name = "key_eq")]
    public string? KeyEq { get; set; }

    public string? Description { get; set; }

    [FromQuery(Name = "created_from")]
    public string? CreatedFrom { get; set; }

    [FromQuery(Name = "created_to")]
    public string? CreatedTo { get; set; }

    [FromQuery(Name = "updated_from")]
    public string? UpdatedFrom { get; set; }

    [FromQuery(Name = "updated_to")]
    public string? UpdatedTo { get; set; }

    [FromQuery(Name = "order_by")]
    public string? OrderBy { get; set; }

    [FromQuery(Name = "order_dir")]
    public string? OrderDir { get; set; }

    public int? Page { get; set; }
    public int? Limit { get; set; }
}
