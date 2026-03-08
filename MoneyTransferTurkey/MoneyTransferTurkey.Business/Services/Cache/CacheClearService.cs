using MoneyTransferTurkey.Business.Infrastructure.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Services.Cache
{
    public class CacheClearService : ICacheClearService
    {
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource _resetToken = new();

        public CancellationToken ResetToken => _resetToken.Token;

        public CacheClearService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ClearAllCaches()
        {
            // Cancel all cache entries that use this token
            _resetToken.Cancel();
            _resetToken.Dispose();
            _resetToken = new CancellationTokenSource();
        }
    }

}
