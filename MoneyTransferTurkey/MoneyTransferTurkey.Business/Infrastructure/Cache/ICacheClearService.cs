using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.Cache
{
    public interface ICacheClearService
    {
        void ClearAllCaches();
    }
}
