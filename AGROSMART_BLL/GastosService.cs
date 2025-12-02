using AGROSMART_DAL;
using AGROSMART_ENTITY.ENTIDADES_DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AGROSMART_BLL
{
    public class GastosService 
    {
        private readonly GastoRepository _repo;

        public GastosService()
        {
            _repo = new GastoRepository();
        }

        public List<GASTOS_DTO> ListarGastos()
        {
            try
            {
                return _repo.ListarGastos();
            }
            catch (Exception ex)
            {
                
                throw new Exception($"Error en el servicio al listar gastos: {ex.Message}", ex);
            }
        }

        public bool EliminarGasto(int idTarea)
        {
            try
            {
                if (idTarea <= 0)
                    throw new ArgumentException("ID de tarea inválido");

                return _repo.EliminarGasto(idTarea);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en el servicio al eliminar gasto: {ex.Message}", ex);
            }
        }

        public GASTOS_DTO ObtenerGastoPorId(int idTarea)
        {
            try
            {
                if (idTarea <= 0)
                    throw new ArgumentException("ID de tarea inválido");

                return _repo.ObtenerGastoPorId(idTarea);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error en el servicio al obtener gasto: {ex.Message}", ex);
            }
        }

        public decimal CalcularTotalGeneral()
        {
            var gastos = ListarGastos();
            return gastos.Sum(g => g.TotalGasto);
        }

        public decimal CalcularTotalPorTipo(string tipo)
        {
            var gastos = ListarGastos();
            switch (tipo.ToUpper())
            {
                case "INSUMOS":
                    return gastos.Sum(g => g.GastoInsumos);
                case "PERSONAL":
                case "EMPLEADOS":
                    return gastos.Sum(g => g.PagoEmpleados);
                case "TRANSPORTE":
                    return gastos.Sum(g => g.GastoTransporte);
                default:
                    return 0;
            }
        }
    }
}
