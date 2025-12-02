using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_ENTITY.ENTIDADES
{
    public class USUARIO
    {
        public int ID_USUARIO { get; set; }      
        public string PRIMER_NOMBRE { get; set; }
        public string SEGUNDO_NOMBRE { get; set; }
        public string PRIMER_APELLIDO { get; set; }
        public string SEGUNDO_APELLIDO { get; set; }
        public string EMAIL { get; set; }
        public string CONTRASENA { get; set; }
        public string TELEFONO { get; set; }

    }
}
