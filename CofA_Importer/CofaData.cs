using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace CofA_Importer
{
    public class CofaData
    {
        #region Properties
        public Int32 Id { get; set; }
        public String Sku { get; set; } = string.Empty;
        public String Lot { get; set; } = string.Empty;
        public String FileName { get; set; } = string.Empty;
        public DateTime UploadDateTime { get; set; }
        #endregion Properties

        private readonly string _connectionString;

        public CofaData(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public static bool DeleteCofa(string connectionString, Action<string>? log = null)
        {
            var logLines = new List<string>();
            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                if (conn.State == ConnectionState.Closed || conn.State == ConnectionState.Broken)
                {
                    conn.Open();
                }

                using SqlCommand cmd = new SqlCommand("ChemCart_DELETE_ATTACHMENTS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        public static bool InsertCofa(CofaData obj, string connectionString)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                if (conn.State == ConnectionState.Closed || conn.State == ConnectionState.Broken)
                {
                    conn.Open();
                }

                using SqlCommand cmd = new SqlCommand("ChemCart_INSERT_ATTACHMENTS", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Sku", obj.Sku);
                cmd.Parameters.AddWithValue("@Lot", obj.Lot);
                cmd.Parameters.AddWithValue("@Filename", obj.FileName);
                cmd.Parameters.AddWithValue("@UploadDateTime", obj.UploadDateTime);

                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}