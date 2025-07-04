using API.RequestHelpers;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaseApiController : ControllerBase
{
    protected async Task<ActionResult> CreatePagedResult<T>(IGenericRepository<T> repo,
        ISpecification<T> spec, int pageIndex, int pageSize) where T : BaseEntity
    {
        IReadOnlyList<T> items = await repo.ListAsync(spec);
        int count = await repo.CountAsync(spec);

        Pagination<T> pagination = new(pageIndex, pageSize, count, items);

        return Ok(pagination);
    }
}
