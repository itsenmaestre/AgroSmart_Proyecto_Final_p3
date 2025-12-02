using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_ENTITY.ENTIDADES
{
    public class CULTIVO
    {
        public int ID_CULTIVO { get; set; }
        public int ID_ADMIN_SUPERVISOR { get; set; }
        public string NOMBRE_LOTE { get; set; }
        public DateTime FECHA_SIEMBRA { get; set; }
        public DateTime FECHA_COSECHA_ESTIMADA { get; set; }
        public string ALERTA_N8N { get; set; }
    }
}
