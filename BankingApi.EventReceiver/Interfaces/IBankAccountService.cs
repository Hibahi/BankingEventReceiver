using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApi.EventReceiver.Interfaces
{
    public interface IBankAccountService
    {
        Task AddAmount(Guid accountId, decimal amount);
        Task DeductAmount(Guid accountId, decimal amount);
    }
}
