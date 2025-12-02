using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_ENTITY.ENTIDADES_DTOS
{
    public class AsignacionEmpleadoDTO
    {
        public int ID_ASIG_TAREA { get; set; }
        public int ID_TAREA { get; set; }
        public int ID_EMPLEADO { get; set; }

        // ASIGNACION_TAREA
        public string ESTADO_ASIGNACION { get; set; }
        public decimal HORAS_TRABAJADAS { get; set; }
        public decimal JORNADAS_TRABAJADAS { get; set; }
        public decimal PAGO_ACORDADO { get; set; }

        // TAREA
        public string TIPO_ACTIVIDAD { get; set; }
        public System.DateTime FECHA_PROGRAMADA { get; set; }
        public string ESTADO_TAREA { get; set; }
    }
}
