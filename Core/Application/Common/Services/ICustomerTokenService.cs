using Domain.Entities;

namespace Application.Common.Services;

public interface ICustomerTokenService
{
    string GenerateToken(Customer customer);
    string GenerateRefreshToken();
}
