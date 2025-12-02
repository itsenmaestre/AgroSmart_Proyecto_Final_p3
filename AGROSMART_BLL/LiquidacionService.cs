using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES;
using AGROSMART_ENTITY.ENTIDADES_DTOS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AGROSMART_BLL
{
    public class LiquidacionService
    {
        private readonly LiquidaciónRepository _liquidacionRepository;
        private readonly EmpleadoRepository _empleadoRepository;
        private readonly UsuarioRepository _usuarioRepository = new UsuarioRepository();

        public LiquidacionService()
        {
            _liquidacionRepository = new LiquidaciónRepository();
            _empleadoRepository = new EmpleadoRepository();
        }

        /*=====================================================================
             CONSULTAS PRINCIPALES (SOLO LECTURA)
        =====================================================================*/

        public ReadOnlyCollection<Liquidación_DTO> ConsultarTodas()
        {
            var lista = _liquidacionRepository.ConsultarTodas();

            CargarNombres(lista);

            return new ReadOnlyCollection<Liquidación_DTO>(lista);
        }

        public async Task<List<Liquidación_DTO>> ConsultarPorCosechaAsync()
        {
            return await Task.Run(() =>
            {
                var lista = _liquidacionRepository.ConsultarTodas();
                CargarNombres(lista);
                return lista;
            });
        }


        public async Task<List<Liquidación_DTO>> ConsultarPorEmpleadoAsync(int idEmpleado)
        {
            return await Task.Run(() =>
            {
                var lista = _liquidacionRepository.ConsultarPorEmpleado(idEmpleado);
                CargarNombres(lista);
                return lista;
            });
        }

        public async Task<List<Liquidación_DTO>> ConsultarPorRangoFechasAsync(DateTime inicio, DateTime fin)
        {
            return await Task.Run(() =>
            {
                var lista = _liquidacionRepository.ConsultarPorFechas(inicio, fin);
                CargarNombres(lista);
                return lista;
            });
        }

        /*=====================================================================
             CÁLCULOS
        =====================================================================*/

        public (decimal Bruto, decimal Deducciones, decimal Neto) CalcularTotales(List<Liquidación_DTO> lista)
        {
            if (lista == null || !lista.Any())
                return (0, 0, 0);

            decimal bruto = lista.Sum(x => x.PagoBruto);
            decimal ded = lista.Sum(x => x.Deducciones);
            decimal neto = lista.Sum(x => x.PagoNeto);

            return (bruto, ded, neto);
        }

        public decimal TotalPagadoEmpleado(int idEmpleado)
        {
            try
            {
                return _liquidacionRepository.TotalPagadoEmpleado(idEmpleado);
            }
            catch
            {
                return 0;
            }
        }

        /*=====================================================================
             MÉTODOS INTERNOS
        =====================================================================*/

        private void CargarNombres(List<Liquidación_DTO> lista)
        {
            foreach (var liq in lista)
            {
                liq.Nombre = ObtenerNombreEmpleado(liq.IdEmpleado);
            }
        }

        private string ObtenerNombreEmpleado(int idEmpleado)
        {
            try
            {
                var empleado = _empleadoRepository.ObtenerPorId(idEmpleado);
                if (empleado == null) return "Sin asignar";

                var usuario = _usuarioRepository.ObtenerPorId(empleado.ID_USUARIO);
                if (usuario == null) return "Sin asignar";

                return $"{usuario.PRIMER_NOMBRE} {usuario.PRIMER_APELLIDO}".Trim();
            }
            catch
            {
                return "Sin asignar";
            }
        }

        /// <summary>
        /// Obtiene la lista de cultivos que tienen liquidaciones
        /// </summary>
        public List<(int IdCultivo, string NombreCultivo)> ObtenerCultivos()
        {
            try
            {
                return _liquidacionRepository.ObtenerCultivosConLiquidaciones();
            }
            catch
            {
                return new List<(int, string)>();
            }
        }
    }
}
