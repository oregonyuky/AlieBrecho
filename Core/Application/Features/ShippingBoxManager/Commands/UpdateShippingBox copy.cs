using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ShippingBoxManager.Commands;

public class UpdateShippingBoxResult
{
    public ShippingBox? Data { get; set; }
}

public class UpdateShippingBoxRequest : IRequest<UpdateShippingBoxResult>
{
    public string? Id { get; init; }
    public decimal? Width { get; init; }
    public decimal? Length { get; init; }
    public decimal? Height { get; init; }
    public decimal? Weight { get; init; }
    public decimal? InsuranceValue { get; init; }
    public bool? IsActive { get; init; }
}

public class UpdateShippingBoxValidator : AbstractValidator<UpdateShippingBoxRequest>
{
    public UpdateShippingBoxValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Width).GreaterThan(0).When(x => x.Width.HasValue);
        RuleFor(x => x.Length).GreaterThan(0).When(x => x.Length.HasValue);
        RuleFor(x => x.Height).GreaterThan(0).When(x => x.Height.HasValue);
        RuleFor(x => x.Weight).GreaterThan(0).When(x => x.Weight.HasValue);

        RuleFor(x => x.InsuranceValue)
            .GreaterThanOrEqualTo(0)
            .When(x => x.InsuranceValue.HasValue);
    }
}

public class UpdateShippingBoxHandler : IRequestHandler<UpdateShippingBoxRequest, UpdateShippingBoxResult>
{
    private readonly ICommandRepository<ShippingBox> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShippingBoxHandler(
        ICommandRepository<ShippingBox> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateShippingBoxResult> Handle(UpdateShippingBoxRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        if (request.Width.HasValue)
            entity.Width = request.Width;

        if (request.Length.HasValue)
            entity.Length = request.Length;

        if (request.Height.HasValue)
            entity.Height = request.Height;

        if (request.Weight.HasValue)
            entity.Weight = request.Weight;

        if (request.InsuranceValue.HasValue)
            entity.InsuranceValue = request.InsuranceValue;

        if (request.IsActive.HasValue)
            entity.IsActive = request.IsActive.Value;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateShippingBoxResult
        {
            Data = entity
        };
    }
}