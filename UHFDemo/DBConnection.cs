using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace UHFDemo
{
    public class DBConnection
    {
        private DBConnection()
        {
        }

        private string databaseName = string.Empty;
        public string DatabaseName
        {
            get => databaseName;
            set => databaseName = value;
        }

        public string Password { get; set; }
        public MySqlConnection Connection { get; private set; }

        private static DBConnection _instance;
        public static DBConnection Instance()
        {
            return _instance ?? (_instance = new DBConnection());
        }

        public bool IsConnect()
        {
            if (Connection != null) return true;

            if (string.IsNullOrEmpty(databaseName))
                return false;
            var connstring = $"Server=localhost; database={databaseName}; UID=root; password=qLW3uc@admin";
            Connection = new MySqlConnection(connstring);
            Connection.Open();

            return true;
        }

        public void Close()
        {
            Connection.Close();
        }
    }
}
