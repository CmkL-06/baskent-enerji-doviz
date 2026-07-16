using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BaskentEnerji.API.Infrastructure
{
    /// <summary>
    /// In-memory buffer for per-user "last activity" timestamps.
    /// Updated on every authenticated request (cheap, no DB write); flushed to
    /// the database periodically by <see cref="BaskentEnerji.API.HostedServices.UserActivityFlushBackgroundService"/>.
    /// </summary>
    public class UserActivityTracker
    {
        private readonly ConcurrentDictionary<Guid, DateTime> _pending = new();

        public void Touch(Guid userId)
        {
            _pending[userId] = DateTime.UtcNow;
        }

        public Dictionary<Guid, DateTime> DrainSnapshot()
        {
            var snapshot = new Dictionary<Guid, DateTime>(_pending);
            foreach (var key in snapshot.Keys)
            {
                _pending.TryRemove(key, out _);
            }
            return snapshot;
        }
    }
}
