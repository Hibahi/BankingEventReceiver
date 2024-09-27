using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApi.EventReceiver.Models
{
    public class CreditDebit
    {
        public Guid Id { get; set; }
        public string MessageType { get; set; }
        public Guid BankAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
