using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGROSMART_BLL
{
    public class EmpleadoService : ICrudLectura<EMPLEADO>, ICrudEscritura<EMPLEADO>
    {
        private readonly EmpleadoRepository _repo = new EmpleadoRepository();
        private readonly UsuarioRepository _usuarioRepo = new UsuarioRepository();

        public ReadOnlyCollection<EMPLEADO> Consultar()
        {
            return new ReadOnlyCollection<EMPLEADO>(_repo.Consultar().ToList());
        }

        public EMPLEADO ObtenerPorId(int id)
        {
            if (id <= 0)
                throw new ArgumentException("El ID del empleado debe ser mayor a cero.");

            return _repo.ObtenerPorId(id);
        }

        public string Guardar(EMPLEADO entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (entidad.ID_USUARIO <= 0)
                throw new ArgumentException("El ID de usuario es inválido.");

            if (entidad.MONTO_POR_HORA < 0)
                throw new ArgumentException("El monto por hora no puede ser negativo.");

            if (entidad.MONTO_POR_JORNAL < 0)
                throw new ArgumentException("El monto por jornal no puede ser negativo.");

            return _repo.Guardar(entidad);
        }

        public bool Actualizar(EMPLEADO entidad)
        {
            if (entidad == null)
                throw new ArgumentNullException(nameof(entidad));

            if (entidad.ID_USUARIO <= 0)
                throw new ArgumentException("El ID de usuario es inválido.");

            if (entidad.MONTO_POR_HORA < 0)
                throw new ArgumentException("El monto por hora no puede ser negativo.");

            if (entidad.MONTO_POR_JORNAL < 0)
                throw new ArgumentException("El monto por jornal no puede ser negativo.");

            return _repo.Actualizar(entidad);
        }

        public bool Eliminar(EMPLEADO entidad)
        {
            if (entidad == null || entidad.ID_USUARIO <= 0)
                throw new ArgumentException("Empleado inválido para eliminar.");

            return _repo.Eliminar(entidad);
        }



        /// <summary>
        /// Obtiene lista completa de empleados con datos de usuario
        /// </summary>
        public List<EmpleadoConUsuario> ListarEmpleadosConUsuario()
        {
            var empleados = _repo.Consultar();
            var lista = new List<EmpleadoConUsuario>();

            foreach (var emp in empleados)
            {
                var usuario = _usuarioRepo.ObtenerPorId(emp.ID_USUARIO);
                if (usuario != null)
                {
                    lista.Add(new EmpleadoConUsuario
                    {
                        IdUsuario = emp.ID_USUARIO,
                        NombreCompleto = $"{usuario.PRIMER_NOMBRE} {usuario.PRIMER_APELLIDO}",
                        Email = usuario.EMAIL,
                        Telefono = usuario.TELEFONO,
                        MontoPorHora = emp.MONTO_POR_HORA,
                        MontoPorJornal = emp.MONTO_POR_JORNAL
                    });
                }
            }

            return lista;
        }

        /// <summary>
        /// Actualiza tarifas de un empleado (RN-45)
        /// </summary>
        public bool ActualizarTarifas(int idEmpleado, decimal montoPorHora, decimal montoPorJornal)
        {
            if (montoPorHora < 0 || montoPorJornal < 0)
                throw new ArgumentException("Los montos no pueden ser negativos");

            var empleado = _repo.ObtenerPorId(idEmpleado);
            if (empleado == null)
                throw new ArgumentException("Empleado no encontrado");

            empleado.MONTO_POR_HORA = montoPorHora;
            empleado.MONTO_POR_JORNAL = montoPorJornal;

            return _repo.Actualizar(empleado);
        }


        public string EliminarEmpleado(int idEmpleado)
        {
            if (idEmpleado <= 0)
                return "ID de empleado inválido.";

            // Verificar si tiene tareas asignadas
            int tareas = _repo.ContarTareasPorEmpleado(idEmpleado);

            if (tareas > 0)
                return $"No se puede eliminar el empleado porque tiene {tareas} tareas asignadas.";

            // Buscar al empleado
            var empleado = _repo.ObtenerPorId(idEmpleado);
            if (empleado == null)
                return "Empleado no encontrado.";

            // Intentar eliminar
            bool ok = _repo.Eliminar(empleado);

            return ok ? "OK" : "No fue posible eliminar el empleado.";
        }


        /// <summary>
        /// DTO para mostrar empleados con información completa
        /// </summary>
        public class EmpleadoConUsuario
        {
            public int IdUsuario { get; set; }
            public string NombreCompleto { get; set; }
            public string Email { get; set; }
            public string Telefono { get; set; }
            public decimal MontoPorHora { get; set; }
            public decimal MontoPorJornal { get; set; }
        }
    }
}
