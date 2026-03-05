using ChatApp.Application.DTOs.Request;
using ChatApp.Application.Interfaces.Repositories;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Repositories;
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserRepository> _logger;
    public UserRepository(AppDbContext db, ILogger<UserRepository> logger)
    {
        _db = db;
        _logger = logger;
    }
    public async Task<(List<User> Users, int TotalCount)> SearchUsersAsync(Guid currentUserId, SearchUsersRequest request)
    {
        _logger.LogInformation("Searching users with term: {Search}", request.Search);
        var query = _db.Users.Where(u=>!u.IsDeleted && u.Id!=currentUserId);
        if(!string.IsNullOrEmpty(request.Search))
        {
            var search= request.Search.ToLower().Trim();
            query = query.Where(u=>u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
        }
        var totalCount = await query.CountAsync();
        var users=await query.OrderBy(u=>u.FullName).Skip((request.Page-1)*request.PageSize).Take(request.PageSize).ToListAsync();
        return (users, totalCount);
    
    }

    

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
    public async Task<User?> GetIdByAsync(Guid id)
    {
        return await _db.Users.FindAsync(id);
    }
}
