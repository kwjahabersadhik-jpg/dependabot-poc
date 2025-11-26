using Kfh.Aurora.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreditCardsSystem.Domain.Interfaces
{
    public interface IAppLog<T> where T : class
    {
        IAuditLogger<T> Log { get; }
    }

 
}
