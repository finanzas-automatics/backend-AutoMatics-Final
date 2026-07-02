namespace AutoMatics.Shared.Responses
{
    public class ApiResponse<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T data, string mensaje = "Operación exitosa") => 
            new() { Exito = true, Mensaje = mensaje, Data = data };

        public static ApiResponse<T> Fail(string mensaje) => 
            new() { Exito = false, Mensaje = mensaje, Data = default };
    }
}