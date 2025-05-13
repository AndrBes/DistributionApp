using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORToolsSampleWinForms.Models
{
    public class FurnaceData
    {
        public int Id { get; set; }
        public double BaseGasFlow { get; set; }       // V_i0^ПГ (строка 3)
        public double MinGasFlow { get; set; }        // V_i,min^ПГ (строка 4)
        public double MaxGasFlow { get; set; }        // V_i,max^ПГ (строка 5)
        public double BaseCokeConsumption { get; set; } // K_0i (строка 6)
        public double CokeEquiv { get; set; }         // e_i (строка 7)
        public double BaseIronProduction { get; set; } // П_i0 (строка 8)
        public double BaseSilicon { get; set; }       // Si_i0 (строка 9)
        public double MinSilicon { get; set; }        // Si_i,min (строка 10)
        public double MaxSilicon { get; set; }        // Si_i,max (строка 11)
        public double DeltaIronGas { get; set; }       // ΔП_i^ПГ (строка 13)
        public double DeltaIronCoke { get; set; }     // ΔП_i^К (строка 14)
        public double DeltaSiGas { get; set; }         // ΔSi_i^ПГ (строка 15)
        public double DeltaSiCoke { get; set; }       // ΔSi_i^К (строка 16)
        public double DeltaSiProduction { get; set; } // ΔSi_i^П (строка 17)

        // РАСЧЁТНЫЕ ДАННЫЕ
        public double Ai { get; set; }                // Коэффициент A_i 
        public double Xmin { get; set; }              // xmin 
        public double Xmax { get; set; }              // xmax 
        public double VpgMax { get; set; }            // Vпгmax 
        public double VpgMin { get; set; }            // Vпгmin 
    }
}
