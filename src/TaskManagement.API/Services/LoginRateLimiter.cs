using System.Collections.Concurrent;

namespace TaskManagement.API.Services;

public class LoginRateLimiter
{
    private readonly ConcurrentDictionary<string, LoginAttempts> _store = new();
    private readonly int _maxAttempts;
    private readonly TimeSpan _lockoutDuration;

    public LoginRateLimiter(int maxAttempts = 5, int lockoutMinutes = 15)
    {
        _maxAttempts = maxAttempts;
        _lockoutDuration = TimeSpan.FromMinutes(lockoutMinutes);
    }

    public bool IsLockedOut(string email)
    {
        if (_store.TryGetValue(email, out var attempts))
        {
            if (attempts.Count >= _maxAttempts)
            {
                if (DateTime.UtcNow - attempts.LastAttempt < _lockoutDuration)
                    return true;
                _store.TryRemove(email, out _);
            }
        }
        return false;
    }

    public void RecordFailedAttempt(string email)
    {
        var attempts = _store.GetOrAdd(email, _ => new LoginAttempts());
        lock (attempts)
        {
            attempts.Count++;
            attempts.LastAttempt = DateTime.UtcNow;
        }
    }

    public void Reset(string email) => _store.TryRemove(email, out _);

    private class LoginAttempts
    {
        public int Count { get; set; }
        public DateTime LastAttempt { get; set; }
    }
}
