using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_BLL
{
    public interface ICrudEscritura<T>
    {
        string Guardar(T entidad);
        bool Actualizar(T entidad);
        bool Eliminar(T entidad);
    }
}
