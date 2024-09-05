using System.Data.SqlClient;

namespace SistemaDoacaoSangue.Data
{
    public class StrConnection
    {
        public static string Connection { get; set; } = "nao pode ser vazia";

        public static void BuildConnection(string _DataSource, string _InitialCatalog, string _UserID, string _Password)
        {
            Connection = new SqlConnectionStringBuilder()
            {
                DataSource = _DataSource,
                InitialCatalog = _InitialCatalog,
                UserID = _UserID,
                Password = _Password,
                PersistSecurityInfo = true,
                MultipleActiveResultSets = true,
                TrustServerCertificate = true,
            }.ConnectionString;
        }
    }
}