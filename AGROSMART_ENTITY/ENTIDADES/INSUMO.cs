using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_ENTITY.ENTIDADES
{
    public class INSUMO
    {
        public int ID_INSUMO { get; set; }
        public int ID_ADMIN_REGISTRO { get; set; }
        public string NOMBRE { get; set; }
        public string TIPO { get; set; }
        public decimal STOCK_ACTUAL { get; set; }   // NUMBER(14,2)
        public decimal STOCK_MINIMO { get; set; }   // NUMBER(14,2)
        public decimal COSTO_UNITARIO { get; set; }
        public string UNIDAD_MEDIDA { get; set; } 
        public DateTime FECHA_ULTIMA_ACTUALIZACION { get; set; }
    }
}
