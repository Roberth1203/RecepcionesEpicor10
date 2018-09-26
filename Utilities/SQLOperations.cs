using System;
using System.Data;
using System.Data.SqlClient;

namespace Utilities
{
    public class SQLOperations
    {
        public String ExceptionCollector { get; set; }
        public static String connectionString;

        public SQLOperations(String Connection)
        {
            connectionString = Connection;
        }

        private static SqlConnection openConnection()
        {
            try
            {
                SqlConnection connector = new SqlConnection(connectionString);
                connector.Open();
                return connector;
            }
            catch (SqlException s)
            {
                Console.WriteLine(String.Format("openConnection > SQLException [{0}] \n\n ExceptionDescription -> {1}", s.Message, s.StackTrace));
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("openConnection > SystemException [{0}] \n\n ExceptionDescription -> {1}", e.Message, e.StackTrace));
                return null;
            }
        }

        private void closeConnection(SqlConnection connector)
        {
            try
            {
                connector.Close();
                SqlConnection.ClearPool(connector);
            }
            catch (SqlException s)
            {
                Console.WriteLine(String.Format("closeConnection > SQLException [{0}] \n\n ExceptionDescription -> {1}", s.Message, s.StackTrace));
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("closeConnection > SystemException [{0}] \n\n ExceptionDescription -> {1}", e.Message, e.StackTrace));
            }
        }

        public DataTable getRecords(String statement, SqlConnection connector = null)
        {
            ExceptionCollector = String.Empty;
            DataTable dt = new DataTable();
            try
            {
                if (connector == null)
                {
                    connector = openConnection();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand(statement, connector);
                    adapter.Fill(dt);
                    closeConnection(connector);
                }
            }
            catch (SqlException s)
            {
                Console.WriteLine(String.Format("getRecords > SQLException [{0}] \n\n ExceptionDescription -> {1}", s.Message, s.StackTrace));
                ExceptionCollector = String.Format("getRecords > SQLException [{0}] \n\n ExceptionDescription -> {1}", s.Message, s.StackTrace);
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("getRecords > SystemException [{0}] \n\n ExceptionDescription -> {1}", e.Message, e.StackTrace));
                ExceptionCollector = String.Format("getRecords > SystemException [{0}] \n\n ExceptionDescription -> {1}", e.Message, e.StackTrace);
            }

            return dt;
        }

        public void execOperation(String sentence, SqlConnection connector = null)
        {
            ExceptionCollector = String.Empty;
            try
            {
                if (connector == null)
                {
                    connector = openConnection();
                    SqlCommand operation = new SqlCommand(sentence, connector);
                    operation.ExecuteNonQuery();
                    closeConnection(connector);
                }
            }
            catch (SqlException s)
            {
                Console.WriteLine(String.Format("execOperation > SystemException [{0}] \n\n ExceptionDescription -> {1}", s.Message, s.StackTrace));
                ExceptionCollector = String.Format("execOperation > SystemException [{0}] \n\n ExceptionDescription -> {1}", s.Message, s.StackTrace);
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("exeOperation > SystemException [{0}] \n\n ExceptionDescription -> {1}", e.Message, e.StackTrace));
                ExceptionCollector = String.Format("exeOperation > SystemException [{0}] \n\n ExceptionDescription -> {1}", e.Message, e.StackTrace);
            }
        }
    }
}
