using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORToolsSampleWinForms.Models
{
    public class WorkshopData
    {
        public double TotalGasReserve { get; set; }   // Резерв ПГ (B21)
        public double TotalCokeReserve { get; set; }  // Запасы кокса (B22)
        public double RequiredIron { get; set; }      // Требуемый чугун (B23)
    }
}
