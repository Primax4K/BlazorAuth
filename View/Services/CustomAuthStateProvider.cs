using View.Options;

namespace View.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider {
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    private readonly ProtectedLocalStorage _local;

    private readonly ILogger<CustomAuthStateProvider> _logger;
    private readonly ISessionRepository _sessionRepository;

    private readonly IUserRepository _userRepository;
    private DateTime _cachedStateTime;
    
    // cache the current AuthenticationState to avoid unnecessary calls to the server
    // the state is cached for 5 minutes
    public AuthenticationState? CachedState;
    

    public CustomAuthStateProvider(IUserRepository userRepository, ProtectedLocalStorage local,
        ILogger<CustomAuthStateProvider> logger, ISessionRepository sessionRepository) {
        _userRepository = userRepository;
        _local = local;
        _logger = logger;
        _sessionRepository = sessionRepository;
    }

    public User? CurrentUser { get; private set; }

    private static AuthenticationState Anonymous => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private void SetCachedState(AuthenticationState state) {
        CachedState = state;
        _cachedStateTime = DateTime.Now;
    }

    private void ClearCache() {
        CachedState = null;
        _cachedStateTime = DateTime.MaxValue;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync() {
        try {
            // caching
            if (CachedState is not null && DateTime.Now - _cachedStateTime < _cacheDuration)
                return CachedState;
            ClearCache();

            var user = await GetUserAsync();
            if (user is null) return Anonymous;
            CurrentUser = user;

            var identity = new ClaimsIdentity(GenerateClaims(user), "GetStateType");
            var authState = new AuthenticationState(new ClaimsPrincipal(identity));

            SetCachedState(authState);
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
            return authState;
        }
        catch (CryptographicException) {
            await _local.DeleteAsync("token");
            return Anonymous; // token encryption has changed
        }
        catch (InvalidOperationException) {
            return Anonymous; // most likely because of JavaScript interop
        }
        catch (Exception e) {
            _logger.LogError(e, "Error while getting authentication state");
            return Anonymous;
        }
    }

    private async Task<User?> GetUserAsync() {
        var token = await _local.GetAsync<string>("token");
        if (token is { Success: true, Value: not "" }) {
            var id = await _sessionRepository.GetUserIdByTokenAsync(token.Value!, CancellationToken.None);

            if (id == null) return null;
            if (!(await _sessionRepository.IsValidSessionAsync(token.Value!, id.Value, CancellationToken.None)))
                return null;

            return await _userRepository.AuthorizeAsync(id.Value, CancellationToken.None);
        }

        return null;
    }

    private static IEnumerable<Claim> GenerateClaims(User user) {
        var claims = new List<Claim> {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };
        claims.AddRange(user.PlainRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        return claims;
    }

    public async Task Login(User user) {
        ClearCache();
        CurrentUser = user;
        
        var salt = BC.GenerateSalt(8);
        var token = BC.HashPassword(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), salt)[..50];

        await _sessionRepository.CreateAsync(new Session() {
            Token = token,
            CreatedAt = DateTime.Now,
            ValidUntil = DateTime.Now.Add(AuthOptions.TokenValidFor),
            UserId = CurrentUser!.Id
        }, CancellationToken.None);
        
        var authState =
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(GenerateClaims(user), "LoginType")));

        SetCachedState(authState);

        NotifyAuthenticationStateChanged(Task.FromResult(authState));
        await _local.SetAsync("token", token);
    }

    public async Task Logout() {
        CurrentUser = null;
        ClearCache();
        await _local.DeleteAsync("token");
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}