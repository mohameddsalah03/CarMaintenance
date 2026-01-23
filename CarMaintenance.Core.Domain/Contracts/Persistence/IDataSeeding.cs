using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarMaintenance.Core.Domain.Contracts.Persistence
{
    public interface IDataSeeding
    {
        Task InitializeAsync();

        Task DataSeedAsync();
    }
}
