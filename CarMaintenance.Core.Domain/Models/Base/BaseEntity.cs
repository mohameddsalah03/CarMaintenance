# nullable disable

namespace CarMaintenance.Core.Domain.Models.Base
{
    public abstract class BaseEntity<TKey> 
    {
        public TKey Id { get; set; } 

    }
}
