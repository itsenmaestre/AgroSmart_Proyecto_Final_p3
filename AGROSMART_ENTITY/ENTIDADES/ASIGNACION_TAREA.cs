using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_ENTITY.ENTIDADES
{
    public class ASIGNACION_TAREA
    {
        public int ID_ASIG_TAREA { get; set; }
        public int ID_TAREA { get; set; }
        public int ID_EMPLEADO { get; set; }
        public int ID_ADMIN_ASIGNADOR { get; set; }
        public System.DateTime FECHA_ASIGNACION { get; set; }
        public string ESTADO { get; set; }

        
        public decimal? HORAS_TRABAJADAS { get; set; }
        public decimal? JORNADAS_TRABAJADAS { get; set; }
        public decimal? PAGO_ACORDADO { get; set; }
    }
}
