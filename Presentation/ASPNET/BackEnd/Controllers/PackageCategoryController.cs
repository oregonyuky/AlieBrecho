using Application.Features.PackageCategoryManager.Commands;
using Application.Features.PackageCategoryManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Authorize]
[Route("api/[controller]")]
public sealed class PackageCategoryController(ISender sender) : BaseApiController(sender)
{
    [HttpGet("GetPackageCategoryList")]
    public async Task<ActionResult<ApiSuccessResult<GetPackageCategoryListResult>>> List(
        CancellationToken cancellationToken, [FromQuery] bool activeOnly = false)
    {
        var content = await _sender.Send(new GetPackageCategoryListRequest { ActiveOnly = activeOnly }, cancellationToken);
        return Ok(new ApiSuccessResult<GetPackageCategoryListResult> { Code = 200, Message = "Categorias de embalagem listadas.", Content = content });
    }

    [HttpGet("GetPackageCategorySingle")]
    public async Task<ActionResult<ApiSuccessResult<PackageCategoryDto?>>> Single(string id, CancellationToken cancellationToken)
    {
        var content = await _sender.Send(new GetPackageCategorySingleRequest { Id = id }, cancellationToken);
        return Ok(new ApiSuccessResult<PackageCategoryDto?> { Code = 200, Message = "Categoria de embalagem encontrada.", Content = content });
    }

    [HttpPost("CreatePackageCategory")]
    public async Task<ActionResult<ApiSuccessResult<SavePackageCategoryResult>>> Create(CreatePackageCategoryRequest request, CancellationToken cancellationToken)
    {
        var content = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<SavePackageCategoryResult> { Code = 200, Message = "Categoria de embalagem criada.", Content = content });
    }

    [HttpPost("UpdatePackageCategory")]
    public async Task<ActionResult<ApiSuccessResult<SavePackageCategoryResult>>> Update(UpdatePackageCategoryRequest request, CancellationToken cancellationToken)
    {
        var content = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<SavePackageCategoryResult> { Code = 200, Message = "Categoria de embalagem atualizada.", Content = content });
    }

    [HttpPost("DeactivatePackageCategory")]
    public async Task<ActionResult<ApiSuccessResult<SavePackageCategoryResult>>> Deactivate(DeactivatePackageCategoryRequest request, CancellationToken cancellationToken)
    {
        var content = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<SavePackageCategoryResult> { Code = 200, Message = "Categoria de embalagem desativada.", Content = content });
    }
}
