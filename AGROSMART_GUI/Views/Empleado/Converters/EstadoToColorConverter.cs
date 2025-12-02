using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AGROSMART_GUI.Converters
{
    public class EstadoToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0 || values[0] == null)
                return Colors.Gray;

            string estado = values[0].ToString();

            switch (estado)
            {
                case "PENDIENTE":
                    return Color.FromRgb(243, 156, 18); // Naranja
                case "EN_EJECUCION":
                    return Color.FromRgb(33, 150, 243); // Azul
                case "FINALIZADA":
                    return Color.FromRgb(76, 175, 80); // Verde
                default:
                    return Colors.Gray;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}