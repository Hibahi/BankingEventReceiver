using BankingApi.EventReceiver.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApi.EventReceiver.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly BankingApiDbContext _dbContext;

        public BankAccountService(BankingApiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAmount(Guid accountId, decimal amount)
        {
            var account = await _dbContext.BankAccounts.FindAsync(accountId);
            if (account == null)
                throw new Exception("Account not found");

            account.Balance += amount;

            await _dbContext.SaveChangesAsync();
        }

        public async Task DeductAmount(Guid accountId, decimal amount)
        {
            var account = await _dbContext.BankAccounts.FindAsync(accountId);
            if (account == null)
                throw new Exception("Account not found");

            if (account.Balance < amount)
                throw new Exception("Insufficient balance");

            account.Balance -= amount;

            await _dbContext.SaveChangesAsync();
        }
    }
}
