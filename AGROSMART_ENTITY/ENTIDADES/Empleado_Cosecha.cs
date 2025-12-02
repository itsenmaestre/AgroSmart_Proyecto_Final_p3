using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_ENTITY.ENTIDADES
{
    public class EMPLEADO_COSECHA
    {
        public int ID_EMPLEADO_COSECHA { get; set; }
        public int ID_EMPLEADO { get; set; }
        public int ID_COSECHA { get; set; }
        public decimal CANTIDAD_COSECHADA { get; set; }
        
        // Campos de pago (configurables)
        public decimal VALOR_UNITARIO { get; set; }
        public decimal PRECIO_BRUTO { get; set; }
        public decimal DEDUCCIONES { get; set; }
        public decimal PRECIO_NETO { get; set; }
        
        // Campos adicionales
        public DateTime FECHA_TRABAJO { get; set; }
        public string OBSERVACIONES { get; set; }
    }
}
