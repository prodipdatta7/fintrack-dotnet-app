using fintrack_netcore_app.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace fintrack_netcore_app.Domain.Entities
{
    public class Transaction: EntityBase
    {
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
    }
}
