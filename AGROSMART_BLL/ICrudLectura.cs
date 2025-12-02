using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_BLL
{
    public interface ICrudLectura<T>
    {
        ReadOnlyCollection<T> Consultar();
        T ObtenerPorId(int id);
    }
}
