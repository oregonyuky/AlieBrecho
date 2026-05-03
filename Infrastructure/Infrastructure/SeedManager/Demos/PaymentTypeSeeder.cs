using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class PaymentTypeSeeder
{
    private readonly ICommandRepository<PaymentType> _paymentTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentTypeSeeder(
        ICommandRepository<PaymentType> paymentTypeRepository,
        IUnitOfWork unitOfWork
    )
    {
        _paymentTypeRepository = paymentTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var paymentTypes = new List<PaymentType>
        {
            new PaymentType { TypeName = "Credit Card", Description = "Payment via credit card." },
            new PaymentType { TypeName = "Debit Card", Description = "Payment via debit card." },
            new PaymentType { TypeName = "Cash", Description = "Payment with cash." },
            new PaymentType { TypeName = "Bank Transfer", Description = "Payment by bank transfer." }
        };

        foreach (var paymentType in paymentTypes)
        {
            await _paymentTypeRepository.CreateAsync(paymentType);
        }

        await _unitOfWork.SaveAsync();
    }
}
