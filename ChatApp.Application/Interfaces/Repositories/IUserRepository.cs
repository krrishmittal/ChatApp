using ChatApp.Application.DTOs.Request;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces.Repositories;
public interface IUserRepository
{
    Task<(List<User> Users, int TotalCount)> SearchUsersAsync(Guid currentUserId, SearchUsersRequest request);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetIdByAsync(Guid id);
}