using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesForecastSystem.Core.Common;
using SalesForecastSystem.Core.DTOs.Users;
using SalesForecastSystem.Core.Helpers;
using SalesForecastSystem.Core.Interfaces.Services;

namespace SalesForecastSystem.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class UserController(IUserService userService) : ApiControllerBase
{
    [HttpGet("next-code")]
    public async Task<IActionResult> GetNextCodeAsync(CancellationToken cancellationToken) =>
        Ok(new { code = await userService.GetNextCodeAsync(cancellationToken) });

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken) =>
        FromServiceResult(await userService.DeleteAsync(id, CurrentUserId(), cancellationToken), _ => NoContent());
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPagedAsync(
        [FromQuery] UserQueryRequest request,
        CancellationToken cancellationToken)
    {
        var users = await userService.GetPagedAsync(request, cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:int}", Name = "GetUserById")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var result = await userService.GetByIdAsync(id, cancellationToken);
        return FromServiceResult(result, Ok);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        UserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.CreateAsync(request, cancellationToken);
        return FromServiceResult(
            result,
            user => CreatedAtRoute("GetUserById", new { id = user.UserId }, user));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAsync(
        int id,
        UserUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.UpdateAsync(id, CurrentUserId(), request, cancellationToken);
        return FromServiceResult(result, Ok);
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatusAsync(
        int id,
        UserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.SetStatusAsync(id, CurrentUserId(), request, cancellationToken);
        return FromServiceResult(result, Ok);
    }

    [HttpPost("{id:int}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPasswordAsync(
        int id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.ResetPasswordAsync(id, request, cancellationToken);
        return FromServiceResult(
            result,
            _ => Ok(new { message = "Password reset successfully. Active sessions were revoked." }));
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirst("sub")!.Value, CultureInfo.InvariantCulture);
}
