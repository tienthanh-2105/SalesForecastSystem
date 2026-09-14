using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SalesForecastSystem.Core.DTOs.Auth;

namespace SalesForecastSystem.Core.Interfaces.Services
{
    public interface ITokenService
    {
        Task<LoginResponse> CreateAccessTokenAsync(LoginUserResponse user, CancellationToken cancellationToken = default);
    }
}
