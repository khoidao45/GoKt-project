using Gokt.Application.DTOs;
using Gokt.Application.Interfaces;
using Gokt.Domain.Entities;
using Gokt.Domain.Enums;
using Gokt.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gokt.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Produces("application/json")]
[Authorize(Roles = "ADMIN")]
public class AdminController(
    IUserRepository userRepository,
    IDriverRepository driverRepository,
    ITripRepository tripRepository,
    IPricingRepository pricingRepository,
    IRoleRepository roleRepository,
    IUnitOfWork unitOfWork) : ControllerBase
{
    // GET /api/v1/admin/stats
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AdminStatsDto), 200)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var totalUsers = await userRepository.CountAsync(ct);
        var totalDrivers = await driverRepository.CountAsync(null, ct);
        var pendingDrivers = await driverRepository.CountAsync(DriverStatus.Pending, ct);
        var activeDrivers = await driverRepository.CountAsync(DriverStatus.Active, ct);
        var totalTrips = await tripRepository.CountAsync(ct);

        return Ok(new AdminStatsDto(totalUsers, totalDrivers, pendingDrivers, activeDrivers, totalTrips));
    }

    // GET /api/v1/admin/users?page=1&pageSize=20
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), 200)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var users = await userRepository.GetAllAsync(page, pageSize, ct);
        var total = await userRepository.CountAsync(ct);
        return Ok(new PagedResult<UserDto>(users.Select(UserDto.From).ToList(), total, page, pageSize));
    }

    // PUT /api/v1/admin/users/{id}/status
    [HttpPut("users/{id:guid}/status")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> SetUserStatus(Guid id, [FromBody] SetStatusRequest req, CancellationToken ct)
    {
        var user = await userRepository.GetByIdWithRolesAsync(id, ct)
            ?? throw new NotFoundException("User", id);

        if (req.Status == "Active") user.Activate();
        else if (req.Status == "Suspended") user.Suspend();
        else return BadRequest(new { message = "Status must be 'Active' or 'Suspended'." });

        await unitOfWork.SaveChangesAsync(ct);
        return Ok(UserDto.From(user));
    }

    // GET /api/v1/admin/drivers?page=1&pageSize=20&status=Pending
    [HttpGet("drivers")]
    [ProducesResponseType(typeof(PagedResult<AdminDriverDto>), 200)]
    public async Task<IActionResult> GetDrivers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        DriverStatus? statusFilter = status switch
        {
            "Pending" => DriverStatus.Pending,
            "Active" => DriverStatus.Active,
            "Suspended" => DriverStatus.Suspended,
            _ => null
        };

        var drivers = await driverRepository.GetAllAsync(page, pageSize, statusFilter, ct);
        var total = await driverRepository.CountAsync(statusFilter, ct);
        var dtos = drivers.Select(d => AdminDriverDto.From(d)).ToList();
        return Ok(new PagedResult<AdminDriverDto>(dtos, total, page, pageSize));
    }

    // PUT /api/v1/admin/drivers/{id}/approve
    [HttpPut("drivers/{id:guid}/approve")]
    [ProducesResponseType(typeof(AdminDriverDto), 200)]
    public async Task<IActionResult> ApproveDriver(Guid id, CancellationToken ct)
    {
        var driver = await driverRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Driver", id);

        var adminId = CurrentUserId;

        driver.Activate();
        await driverRepository.UpdateAsync(driver, ct);

        // Assign DRIVER role if not already assigned
        var user = await userRepository.GetByIdWithRolesAsync(driver.UserId, ct)
            ?? throw new NotFoundException("User", driver.UserId);

        if (!user.GetRoleNames().Contains("DRIVER"))
        {
            var driverRole = await roleRepository.GetByNameAsync("DRIVER", ct)
                ?? throw new InvalidOperationException("DRIVER role not seeded in database.");
            user.UserRoles.Add(UserRole.Create(user.Id, driverRole.Id, adminId));
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Ok(AdminDriverDto.From(driver));
    }

    // PUT /api/v1/admin/drivers/{id}/reject
    [HttpPut("drivers/{id:guid}/reject")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RejectDriver(Guid id, CancellationToken ct)
    {
        var driver = await driverRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Driver", id);

        driver.Suspend();
        await driverRepository.UpdateAsync(driver, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    // PUT /api/v1/admin/drivers/{id}/suspend
    [HttpPut("drivers/{id:guid}/suspend")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SuspendDriver(Guid id, CancellationToken ct)
    {
        var driver = await driverRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Driver", id);

        driver.Suspend();
        await driverRepository.UpdateAsync(driver, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }

    // GET /api/v1/admin/trips?page=1&pageSize=20
    [HttpGet("trips")]
    [ProducesResponseType(typeof(PagedResult<TripDto>), 200)]
    public async Task<IActionResult> GetTrips(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var trips = await tripRepository.GetAllAsync(page, pageSize, ct);
        var total = await tripRepository.CountAsync(ct);
        return Ok(new PagedResult<TripDto>(trips.Select(TripDto.From).ToList(), total, page, pageSize));
    }

    // GET /api/v1/admin/pricing
    [HttpGet("pricing")]
    [ProducesResponseType(typeof(IEnumerable<PricingRuleDto>), 200)]
    public async Task<IActionResult> GetPricing(CancellationToken ct)
    {
        var rules = await pricingRepository.GetAllAsync(ct);
        return Ok(rules.Select(PricingRuleDto.From));
    }

    // PUT /api/v1/admin/pricing/{id}
    [HttpPut("pricing/{id:guid}")]
    [ProducesResponseType(typeof(PricingRuleDto), 200)]
    public async Task<IActionResult> UpdatePricing(Guid id, [FromBody] UpdatePricingRequest req, CancellationToken ct)
    {
        var rules = await pricingRepository.GetAllAsync(ct);
        var rule = rules.FirstOrDefault(r => r.Id == id);
        if (rule is null) return NotFound(new { message = "Pricing rule not found." });

        rule.Update(req.BaseFare, req.PerKmRate, req.PerMinuteRate, req.MinimumFare, req.SurgeMultiplier);
        await pricingRepository.UpdateAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Ok(PricingRuleDto.From(rule));
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found."));
}

// ─── Admin-specific DTOs ──────────────────────────────────────────────────────

public record AdminStatsDto(
    int TotalUsers,
    int TotalDrivers,
    int PendingDrivers,
    int ActiveDrivers,
    int TotalTrips
);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize
);

public record AdminDriverDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? Email,
    string? Phone,
    string? AvatarUrl,
    string DriverCode,
    string LicenseNumber,
    DateTime LicenseExpiry,
    DriverStatus Status,
    decimal Rating,
    int TotalRides,
    bool IsOnline,
    List<VehicleDto> Vehicles,
    DateTime CreatedAt
)
{
    public static AdminDriverDto From(Driver d) =>
        new(
            d.Id,
            d.UserId,
            d.User?.Profile != null
                ? $"{d.User.Profile.FirstName} {d.User.Profile.LastName}".Trim()
                : d.UserId.ToString(),
            d.User?.Email,
            d.User?.Phone,
            d.User?.Profile?.AvatarUrl,
            d.DriverCode ?? string.Empty,
            d.LicenseNumber,
            d.LicenseExpiry,
            d.Status,
            d.Rating,
            d.TotalRides,
            d.IsOnline,
            d.Vehicles.Select(VehicleDto.From).ToList(),
            d.CreatedAt
        );
}

public record PricingRuleDto(
    Guid Id,
    string VehicleType,
    decimal BaseFare,
    decimal PerKmRate,
    decimal PerMinuteRate,
    decimal MinimumFare,
    decimal SurgeMultiplier,
    bool IsActive
)
{
    public static PricingRuleDto From(Domain.Entities.PricingRule r) =>
        new(r.Id, r.VehicleType.ToString(), r.BaseFare, r.PerKmRate, r.PerMinuteRate,
            r.MinimumFare, r.SurgeMultiplier, r.IsActive);
}

public record SetStatusRequest(string Status);
public record UpdatePricingRequest(
    decimal BaseFare,
    decimal PerKmRate,
    decimal PerMinuteRate,
    decimal MinimumFare,
    decimal SurgeMultiplier
);
