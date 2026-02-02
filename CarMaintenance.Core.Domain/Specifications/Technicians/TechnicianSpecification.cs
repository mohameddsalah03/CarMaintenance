using CarMaintenance.Core.Domain.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarMaintenance.Core.Domain.Specifications.Technicians
{
    public class TechnicianSpecification : BaseSpecifications<Technician,string>
    {


        public TechnicianSpecification():base()
        {
            AddIncludes();
        }
        public TechnicianSpecification(bool isAvailable) : base(v=>v.IsAvailable == isAvailable)
        {
            AddIncludes();
        }
        public TechnicianSpecification(string id,string userId) : base(
            v=> v.Id == id && v.UserId == userId)
        {
            AddIncludes();
        }

        protected override void AddIncludes()
        {
            Includes.Add(v => v.User);
        }
    }
}
