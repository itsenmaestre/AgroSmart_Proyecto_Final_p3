using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AGROSMART_BLL
{
    public class UsuarioService : ICrudLectura<USUARIO>, ICrudEscritura<USUARIO>
    {
        private readonly UsuarioRepository _uRepo = new UsuarioRepository();

        // ---- LOGIN con ID + contraseña ----
        public USUARIO Login(int idUsuario, string contrasena)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("El ID (cédula) es obligatorio y debe ser numérico.");

            if (string.IsNullOrWhiteSpace(contrasena))
                throw new ArgumentException("La contraseña es obligatoria.");

            var u = _uRepo.Autenticar(idUsuario, contrasena);
            if (u == null)
                throw new InvalidOperationException("Credenciales incorrectas.");

            return u;
        }

        // ---- Registro de empleado: delega transacción a la DAL ----
        public string RegistrarEmpleado(USUARIO u, EMPLEADO e)
        {
            // Validaciones mínimas alineadas con tus CHECKs
            if (u == null) throw new ArgumentNullException(nameof(u));
            if (e == null) throw new ArgumentNullException(nameof(e));

            if (u.ID_USUARIO <= 0) throw new ArgumentException("La cédula (ID_USUARIO) es obligatoria.");
            if (string.IsNullOrWhiteSpace(u.PRIMER_NOMBRE) || string.IsNullOrWhiteSpace(u.PRIMER_APELLIDO))
                throw new ArgumentException("Nombres y apellidos requeridos.");
            if (!Regex.IsMatch(u.EMAIL ?? "", @"^\S+@\S+\.\S+$"))
                throw new ArgumentException("Email inválido.");
            if (!Regex.IsMatch(u.TELEFONO ?? "", @"^[0-9]{7,15}$"))
                throw new ArgumentException("Teléfono inválido.");
            if (string.IsNullOrWhiteSpace(u.CONTRASENA))
                throw new ArgumentException("Contraseña requerida.");

            // La inserción en USUARIO + EMPLEADO se hace en una transacción dentro de la DAL
            return _uRepo.RegistrarEmpleado(u, e);
        }

        // ---- utilidades de rol ----
        public bool EsAdministrador(int idUsuario) => _uRepo.EsAdministrador(idUsuario);
        public bool EsEmpleado(int idUsuario) => _uRepo.EsEmpleado(idUsuario);

        // ---- CRUD estándar (delegado a la DAL) ----
        public ReadOnlyCollection<USUARIO> Consultar()
            => new ReadOnlyCollection<USUARIO>(
                new System.Collections.Generic.List<USUARIO>(_uRepo.Consultar()));

        public USUARIO ObtenerPorId(int id) => _uRepo.ObtenerPorId(id);
        public string Guardar(USUARIO entidad) => _uRepo.Guardar(entidad);
        public bool Actualizar(USUARIO entidad) => _uRepo.Actualizar(entidad);
        public bool Eliminar(USUARIO entidad) => _uRepo.Eliminar(entidad);
    }

}
