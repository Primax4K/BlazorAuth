namespace Domain.Repositories.Interfaces; 

public interface ISessionRepository : IRepository<Session> {
    Task<bool> IsValidSessionAsync(string token, int userId, CancellationToken ct);
    
    Task<int?> GetUserIdByTokenAsync(string token, CancellationToken ct);
}