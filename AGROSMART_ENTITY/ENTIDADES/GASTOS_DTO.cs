using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_ENTITY.ENTIDADES_DTOS
{
    public class GASTOS_DTO
    {

        public int IdTarea { get; set; }
        public string NombreTarea { get; set; }
        public string Cultivo { get; set; }
        public DateTime FechaTarea { get; set; }  // ← NUEVA PROPIEDAD
        public string Estado { get; set; }         // ← NUEVA PROPIEDAD
        public decimal GastoInsumos { get; set; }
        public decimal PagoEmpleados { get; set; }
        public decimal GastoTransporte { get; set; }
        public decimal TotalGasto { get; set; }

    }
}
