using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReposteriasManu.API.Responses
{
    public sealed record ApiErrorResponse(string Message, IDictionary<string, string[]>? Errors = null)
    {
        private static readonly Dictionary<string, string> FieldNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = "Nombre",
            ["LastName"] = "Apellido",
            ["Email"] = "Correo electronico",
            ["Phone"] = "Telefono",
            ["Address"] = "Direccion",
            ["Description"] = "Descripcion",
            ["Price"] = "Precio",
            ["Flavor"] = "Sabor",
            ["Size"] = "Tamano",
            ["OrderDate"] = "Fecha del pedido",
            ["DeliveryDate"] = "Fecha de entrega",
            ["Status"] = "Estado",
            ["Notes"] = "Notas",
            ["CustomerId"] = "Cliente",
            ["Type"] = "Tipo",
            ["Color"] = "Color",
            ["Message"] = "Mensaje",
            ["OrderId"] = "Pedido",
            ["ProductId"] = "Producto"
        };

        public static ApiErrorResponse FromModelState(ModelStateDictionary modelState)
        {
            var errors = modelState
                .Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value!.Errors
                        .Select(error => GetValidationMessage(item.Key, error.ErrorMessage))
                        .ToArray());

            return new ApiErrorResponse("Revise los datos enviados e intente nuevamente.", errors);
        }

        private static string GetValidationMessage(string fieldName, string errorMessage)
        {
            var displayName = GetDisplayName(fieldName);

            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return $"El campo {displayName} no es valido.";
            }

            if (errorMessage.Contains("required", StringComparison.OrdinalIgnoreCase))
            {
                return $"El campo {displayName} es obligatorio.";
            }

            if (errorMessage.Contains("e-mail", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                return $"El campo {displayName} debe tener un formato de correo valido.";
            }

            if (errorMessage.Contains("maximum length", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("MaxLength", StringComparison.OrdinalIgnoreCase))
            {
                return $"El campo {displayName} supera la longitud permitida.";
            }

            if (errorMessage.Contains("range", StringComparison.OrdinalIgnoreCase) ||
                errorMessage.Contains("between", StringComparison.OrdinalIgnoreCase))
            {
                return $"El campo {displayName} esta fuera del rango permitido.";
            }

            return $"El campo {displayName} no es valido.";
        }

        private static string GetDisplayName(string fieldName)
        {
            var normalizedFieldName = fieldName.Split('.').Last();

            return FieldNames.TryGetValue(normalizedFieldName, out var displayName)
                ? displayName
                : normalizedFieldName;
        }
    }
}
