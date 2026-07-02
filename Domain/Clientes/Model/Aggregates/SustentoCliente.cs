namespace AutoMatics.Domain.Clientes.Model.Aggregates
{
    public class SustentoCliente
    {
        public int Id { get; private set; }
        public int ClienteId { get; private set; }
        public string TipoDocumentoSustento { get; private set; } = string.Empty; 
        public string UrlArchivo { get; private set; } = string.Empty;

        protected SustentoCliente() { }

        public SustentoCliente(string tipoDocumentoSustento, string urlArchivo)
        {
            TipoDocumentoSustento = tipoDocumentoSustento;
            UrlArchivo = urlArchivo;
        }
    }
}