using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_ENTITY.ENTIDADES
{
    public class DETALLE_TAREA
    {
        public int ID_DETALLE_TAREA { get; set; }
        public int ID_TAREA { get; set; }
        public int ID_INSUMO { get; set; }
        public decimal CANTIDAD_USADA { get; set; } 
    }
}
