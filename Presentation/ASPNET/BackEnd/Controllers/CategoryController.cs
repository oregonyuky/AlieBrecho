using Application.Features.CategoryManager.Commands;
using Application.Features.CategoryManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class CategoryController : BaseApiController
{
    public CategoryController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateCategory")]
    public async Task<ActionResult<ApiSuccessResult<CreateCategoryResult>>> CreateCategoryAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateCategoryResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateCategoryAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateCategory")]
    public async Task<ActionResult<ApiSuccessResult<UpdateCategoryResult>>> UpdateCategoryAsync(
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateCategoryResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateCategoryAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteCategory")]
    public async Task<ActionResult<ApiSuccessResult<DeleteCategoryResult>>> DeleteCategoryAsync(
        DeleteCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteCategoryResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteCategoryAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCategorySingle")]
    public async Task<ActionResult<ApiSuccessResult<GetCategorySingleResult>>> GetCategorySingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id
        )
    {
        var request = new GetCategorySingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCategorySingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCategorySingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCategoryList")]
    public async Task<ActionResult<ApiSuccessResult<GetCategoryListResult>>> GetCategoryListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetCategoryListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCategoryListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCategoryListAsync)}",
            Content = response
        });
    }
}
