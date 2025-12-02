using System;

namespace AGROSMART_ENTITY.ENTIDADES
{
    public class COSECHA
    {
        public int ID_COSECHA { get; set; }
        public int ID_CULTIVO { get; set; }
        public int ID_ADMIN_REGISTRO { get; set; }

        // ⭐ CAMBIO: FECHA_COSECHA → FECHA_INICIO
        public DateTime FECHA_INICIO { get; set; }

        public DateTime FECHA_REGISTRO { get; set; }

        // ⭐ NUEVO: Fecha en que se finalizó la cosecha
        public DateTime? FECHA_FINALIZACION { get; set; }

        public decimal CANTIDAD_OBTENIDA { get; set; }
        public string UNIDAD_MEDIDA { get; set; }
        public string CALIDAD { get; set; }
        public string OBSERVACIONES { get; set; }
        public string ESTADO { get; set; } // EN_PROCESO | TERMINADA
    }
}