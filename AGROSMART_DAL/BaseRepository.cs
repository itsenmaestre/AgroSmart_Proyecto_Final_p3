using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;

namespace AGROSMART_DAL
{
    public abstract class BaseRepository<T>
    {
        protected OracleConnection CrearConexion()
        {
            return Conexion.CrearConexion();
        }

        // ===== Métodos obligatorios =====
        public abstract IList<T> Consultar();
        public abstract T ObtenerPorId(int id);

        // ===== Métodos opcionales (virtual) =====
        public virtual string Guardar(T entidad)
        {
            throw new NotImplementedException("El repositorio no implementó Guardar().");
        }

        public virtual bool Actualizar(T entidad)
        {
            throw new NotImplementedException("El repositorio no implementó Actualizar().");
        }

        // Forma vieja: eliminar recibiendo la ENTIDAD completa
        public virtual bool Eliminar(T entidad)
        {
            throw new NotImplementedException("El repositorio no implementó Eliminar(T entidad).");
        }

        // Forma nueva: eliminar por ID numérico
        public virtual bool Eliminar(int id)
        {
            throw new NotImplementedException("El repositorio no implementó Eliminar(int id).");
        }
    }
}
