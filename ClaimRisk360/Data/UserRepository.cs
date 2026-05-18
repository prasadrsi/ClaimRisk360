using ClaimRisk360.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimRisk360.Data;

/// <summary>
/// Data layer: manages users and roles via SQLite + EF Core.
/// Includes async methods for performance optimization.
/// </summary>
public class UserRepository
{
    private readonly AppDbContext _db;
    private int _userCounter;

    public UserRepository(AppDbContext db)
    {
        _db = db;
        _userCounter = _db.AppUsers.Count();
    }

    #region Synchronous Methods (Legacy)
    // Roles
    public List<AppRole> GetAllRoles() => _db.AppRoles.Include(r => r.Permissions).ToList();
    public AppRole? GetRole(string roleId) => _db.AppRoles.Include(r => r.Permissions).FirstOrDefault(r => r.RoleId == roleId);

    // Users
    public List<AppUser> GetAllUsers() => _db.AppUsers.OrderBy(u => u.DisplayName).ToList();
    public AppUser? GetUser(string userId) => _db.AppUsers.FirstOrDefault(u => u.UserId == userId);
    public AppUser? GetUserByEmail(string email) => _db.AppUsers.FirstOrDefault(u =>
        EF.Functions.Like(u.Email, email));

    public void AddUser(AppUser user)
    {
        user.UserId = $"USR-{Interlocked.Increment(ref _userCounter):D3}";
        user.CreatedAt = DateTime.UtcNow;
        _db.AppUsers.Add(user);
        _db.SaveChanges();
    }

    public void UpdateUser(string userId, string roleId, string department, bool isActive)
    {
        var user = GetUser(userId);
        if (user is null) return;
        user.RoleId = roleId;
        user.Department = department;
        user.IsActive = isActive;
        _db.SaveChanges();
    }
    #endregion

    #region Async Methods (New - Performance Optimized)

    // Roles
    /// <summary>
    /// Get all roles asynchronously
    /// </summary>
    public async Task<List<AppRole>> GetAllRolesAsync() => 
        await _db.AppRoles.Include(r => r.Permissions).ToListAsync();

    /// <summary>
    /// Get single role asynchronously
    /// </summary>
    public async Task<AppRole?> GetRoleAsync(string roleId) => 
        await _db.AppRoles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.RoleId == roleId);

    // Users
    /// <summary>
    /// Get all users asynchronously (paginated recommended)
    /// </summary>
    public async Task<List<AppUser>> GetAllUsersAsync() => 
        await _db.AppUsers.OrderBy(u => u.DisplayName).ToListAsync();

    /// <summary>
    /// Get paginated users
    /// </summary>
    public async Task<PaginatedResult<AppUser>> GetAllUsersPaginatedAsync(int pageNumber = 1, int pageSize = 50)
    {
        var pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };

        var total = await _db.AppUsers.CountAsync();
        var users = await _db.AppUsers
            .OrderBy(u => u.DisplayName)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync();

        return new PaginatedResult<AppUser>
        {
            Items = users,
            TotalCount = total,
            CurrentPage = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Get user by ID asynchronously
    /// </summary>
    public async Task<AppUser?> GetUserAsync(string userId) => 
        await _db.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);

    /// <summary>
    /// Get user by email asynchronously
    /// </summary>
    public async Task<AppUser?> GetUserByEmailAsync(string email) => 
        await _db.AppUsers.FirstOrDefaultAsync(u => EF.Functions.Like(u.Email, email));

    /// <summary>
    /// Add user asynchronously
    /// </summary>
    public async Task AddUserAsync(AppUser user)
    {
        user.UserId = $"USR-{Interlocked.Increment(ref _userCounter):D3}";
        user.CreatedAt = DateTime.UtcNow;
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Update user asynchronously
    /// </summary>
    public async Task UpdateUserAsync(string userId, string roleId, string department, bool isActive)
    {
        var user = await GetUserAsync(userId);
        if (user is null) return;
        user.RoleId = roleId;
        user.Department = department;
        user.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    #endregion
}
