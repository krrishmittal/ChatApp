using ChatApp.Domain.Entities;

namespace ChatApp.Application.Interfaces.Repositories;
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetIdByAsync(Guid id);

}