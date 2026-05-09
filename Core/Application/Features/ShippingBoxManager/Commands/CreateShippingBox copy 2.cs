using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.ShippingBoxManager.Commands;

public class CreateShippingBoxResult
{
    public ShippingBox? Data { get; set; }
}

public class CreateShippingBoxRequest : IRequest<CreateShippingBoxResult>
{
    public decimal? Width { get; init; }
    public decimal? Length { get; init; }
    public decimal? Height { get; init; }
    public decimal? Weight { get; init; }
    public decimal? InsuranceValue { get; init; }
    public bool? IsActive { get; init; }
}

public class CreateShippingBoxValidator : AbstractValidator<CreateShippingBoxRequest>
{
    public CreateShippingBoxValidator()
    {
        RuleFor(x => x.Width).NotNull().GreaterThan(0);
        RuleFor(x => x.Length).NotNull().GreaterThan(0);
        RuleFor(x => x.Height).NotNull().GreaterThan(0);
        RuleFor(x => x.Weight).NotNull().GreaterThan(0);

        RuleFor(x => x.InsuranceValue)
            .GreaterThanOrEqualTo(0)
            .When(x => x.InsuranceValue.HasValue);
    }
}

public class CreateShippingBoxHandler : IRequestHandler<CreateShippingBoxRequest, CreateShippingBoxResult>
{
    private readonly ICommandRepository<ShippingBox> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateShippingBoxHandler(
        ICommandRepository<ShippingBox> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateShippingBoxResult> Handle(CreateShippingBoxRequest request, CancellationToken cancellationToken)
    {
        var entity = new ShippingBox
        {
            Width = request.Width,
            Length = request.Length,
            Height = request.Height,
            Weight = request.Weight,
            InsuranceValue = request.InsuranceValue ?? 0,
            IsActive = request.IsActive ?? true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateShippingBoxResult
        {
            Data = entity
        };
    }
}