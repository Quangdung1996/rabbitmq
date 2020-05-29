using System;
using System.Collections.Generic;
using System.Text;

namespace Exchange.Common
{
    public class Account
    {
        public int AccountId { get; set; }

        public string Name { get; set; }

        public DateTime DateCreated { get; set; }
    }

    public static class AccountService
    {
        public static Account Load()
        {
            return new Account()
            {
                DateCreated = DateTime.Now,
                Name = $"Test-dev",
                AccountId = 1
            };
        }
    }
}
