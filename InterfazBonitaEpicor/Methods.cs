using System;
using System.Data;
using Utilities;
using Epicor10;
using Ice.Core;
using System.Configuration;
using System.Text;

namespace InterfazBonitaEpicor
{
    class Methods
    {
        AppLogs log = new AppLogs();
        EpicorMethods ERP;
        SQLOperations ops;
        Statements stmt = new Statements();

        // Epicor Info
        static String user = ConfigurationManager.AppSettings["epicorUser"].ToString();
        static String pass = ConfigurationManager.AppSettings["epicorPass"].ToString();
        static String company = ConfigurationManager.AppSettings["companyERP"].ToString();

        public void ProcesoFolioBPM()
        {
            try
            {
                /* 
                 * Busqueda de folios con status = RECIBIDO y FechaSalidaProveedor NOT NULL
                 * Retorna: idFolio, IDProveedor, NumeroOC,Nombre
                 */ 
                 ops = new SQLOperations(String.Format(ConfigurationManager.AppSettings["connBPM"].ToString(), "***", "***"));

                DataTable dtFoliosBPM = ops.getRecords(stmt.GETFOLIOSVERIFICADOS);

                if (dtFoliosBPM.Rows.Count > 0)
                {
                    // Cambio de status a 2 para los Folios encontrados
                    StringBuilder sb = new StringBuilder();
                    foreach (DataRow item in dtFoliosBPM.Rows)
                    {
                        if (sb.Length == 0)
                            sb.Append($"{item[0].ToString()}");
                        else
                            sb.Append($",{item[0].ToString()}");
                    }
                    
                    ops.execOperation(String.Format(stmt.UPDIDPLANEADOS, 2, sb));



                    // Creación del log
                    log.CreateLogFile(DateTime.Now.ToString("dd-MM-yyyy"));

                    // Solicitud de licencia y manejo de adaptadores
                    Session epiSession = new Session(user, pass, Session.LicenseType.EnterpriseProcessing, String.Format(ConfigurationManager.AppSettings["configFile"], "Epicor10"));
                    ERP = new EpicorMethods(user, pass, company);

                    foreach(DataRow fila in dtFoliosBPM.Rows)

                    {
                        Int32 huboErrores = 0;
                        Console.WriteLine(String.Format("Procesando el folio {0}", fila[1].ToString()));
                        log.WriteOnLogFile(String.Format("Procesando el folio {0}", fila[1].ToString()), DateTime.Now);
                        Console.WriteLine(String.Format("Proveedor: {0} - Nombre: {1} - Orden de Compra: {2}", fila[2].ToString(), fila[4].ToString(), fila[3].ToString()));
                        log.WriteOnLogFile(String.Format("Proveedor: {0} - Nombre: {1} - Orden de Compra: {2}", fila[2].ToString(), fila[4].ToString(), fila[3].ToString()));

                        // Se consulta el detalle de la OC para la recepción
                        DataTable dtDetalleOC = ops.getRecords(String.Format(stmt.GETDETALLEOC, fila[1].ToString(), fila[3].ToString())); 

                        if (dtDetalleOC.Rows.Count > 0)
                        {
                            //Internamente se crean las lineas de la recepcion,
                            //en caso de haber error con alguna de ellas la variable huboErrores
                            //obtiene la cantidad de lineas que presentaron problema.
                            //Con esto, decidimos que status poner en CAOR.PLANEADOS (1 = Correcto, 3 = Terminado con excepciones).

                            ERP.CreateReceipt(fila[2].ToString(), "BPM-" + fila[0].ToString(), Convert.ToInt32(fila[3]), dtDetalleOC, out huboErrores);

                            if (huboErrores > 0)
                                ops.execOperation(String.Format(stmt.UPDIDPLANEADOS, 3, fila[0].ToString()));
                            else
                                ops.execOperation(String.Format(stmt.UPDIDPLANEADOS, 1, fila[0].ToString()));
                        }
                    }

                    Console.WriteLine("Hemos terminado.");
                    log.WriteOnLogFile("Ha terminado el proceso de folios a Epicor", DateTime.Now);
                }
                else
                    Console.WriteLine("No hay folios que procesar.");
            }
            catch(System.UnauthorizedAccessException userfile)
            {
                Console.WriteLine(userfile.Message + " - " + userfile.StackTrace);
                Console.Read();
            }
        }
    }
}
