using System;
using System.Configuration;
using System.ServiceModel.Channels;
using Epicor.ServiceModel.StandardBindings;
using Ice.Lib;
using Erp.Proxy.BO;
using Erp.BO;
using System.Data;
using Utilities;

namespace Epicor10
{
    public class EpicorMethods
    {
        public String collector { get; set; }
        protected String fileSys = String.Format(ConfigurationManager.AppSettings["configFile"].ToString(), "Epicor10");
        protected String Company;

        SQLOperations sqlBPM;
        AppLogs log = new AppLogs();
        Credentials cred = new Credentials();
        Statements stmt;

        public EpicorMethods(String user, String pass, String company)
        {
            cred.defaultUser = user;
            cred.defaultPass = pass;
            setCompany(company);
        }

        private void setCompany(string currentCompany)
        {
            try
            {
                collector = String.Empty;
                string appServerUrl = string.Empty;
                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(String.Format("{0}/Ice/BO/{1}.svc", appServerUrl, "UserFile"));

                using (Ice.Proxy.BO.UserFileImpl US = new Ice.Proxy.BO.UserFileImpl(wcfBinding, CustSvcUri))
                {
                    US.ClientCredentials.UserName.UserName = cred.defaultUser;
                    US.ClientCredentials.UserName.Password = cred.defaultPass;
                    US.SaveSettings(cred.defaultUser, true, currentCompany, true, false, true, true, true, true, true, true, true,
                                               false, false, -2, 0, 1456, 886, 2, "MAINMENU", "", "", 0, -1, 0, "", false);
                    US.Close();
                    US.Dispose();
                }
            }
            catch (System.UnauthorizedAccessException loginError)
            {
                collector = loginError.Message;
            }
            catch (Ice.Common.BusinessObjectException BOException)
            {
                collector = BOException.Message;
            }
            catch (Exception error)
            {
                collector = error.Message;
            }
        }

        public void CreateReceipt(String VendorID, String idBPM, Int32 POOrderNum, DataTable detalleOC, out Int32 HasErrors)
        {
            log.CreateLogFile(DateTime.Now.ToString("dd-MM-yyyy"));
            HasErrors = 0;
            try
            {
                sqlBPM = new SQLOperations(String.Format(ConfigurationManager.AppSettings["connBPM"].ToString(), "***", "***"));
                stmt = new Statements();

                collector = String.Empty;
                String purPoint;
                Int32 EpiVendorNum;
                Int32 errors = 0;

                string appServerUrl = string.Empty;
                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(String.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "Receipt"));

                using (Erp.Proxy.BO.ReceiptImpl BOReceipt = new Erp.Proxy.BO.ReceiptImpl(wcfBinding, CustSvcUri))
                {
                    BOReceipt.ClientCredentials.UserName.UserName = cred.defaultUser;
                    BOReceipt.ClientCredentials.UserName.Password = cred.defaultPass;
                    ReceiptDataSet ds = new ReceiptDataSet();

                    //Int32 VendorNum = GetVendorNum(VendorID);

                    BOReceipt.GetNewRcvHead(ds, 0, "");
                    BOReceipt.GetVendorInfo(ds, VendorID, out EpiVendorNum, out purPoint);
                        ds.Tables["RcvHead"].Rows[0]["PackSlip"] = idBPM;
                        ds.Tables["RcvHead"].Rows[0]["PONum"] = POOrderNum;
                        ds.Tables["RcvHead"].Rows[0]["ShipViaCode"] = "CC";
                    BOReceipt.Update(ds);
                        
                    Console.WriteLine("Recepción creada correctamente");
                    log.WriteOnLogFile(String.Format("Encabezado de la recepción: OrderNum {0} - VendorNum {1} - PackSlip {2}", POOrderNum, EpiVendorNum, idBPM));
                    log.WriteOnLogFile(String.Format("Iniciando la generación del detallado para la Orden de compra: {0}", POOrderNum));

                    // Carga de las lineas de OC
                    foreach (DataRow dtlRow in detalleOC.Rows)
                    {
                        String DtlResponse = AddReceiptLine(dtlRow, EpiVendorNum, idBPM, POOrderNum);
                        log.WriteOnLogFile(DtlResponse);

                        if (!DtlResponse.Contains("Status [OK]"))
                        {
                            errors++;
                            sqlBPM.execOperation(String.Format(stmt.UPDSTATUSDETALLES, 3, dtlRow[17].ToString()));
                        }
                        else
                            sqlBPM.execOperation(String.Format(stmt.UPDSTATUSDETALLES, 1, dtlRow[17].ToString()));

                    }
                    HasErrors = errors;
                    
                }
            }
            catch (Ice.Common.DuplicateRecordException epiWarning)
            {
                HasErrors = 1;
                Console.WriteLine("DuplicateRecordException [Method: CreateReceipt] \nMessage: {0} \n StackTrace: {1}", epiWarning.Message, epiWarning.StackTrace);
                log.WriteOnLogFile(String.Format("DuplicateRecordException [Method: CreateReceipt] \nMessage: {0} \n StackTrace: {1}", epiWarning.Message, epiWarning.StackTrace));
            }
            catch (Ice.Common.EpicorServerException epiError)
            {
                HasErrors = 1;
                Console.WriteLine("EpicorServerException [Method: CreateReceipt] \nMessage: {0} \n StackTrace: {1}", epiError.Message, epiError.StackTrace);
                log.WriteOnLogFile(String.Format("EpicorServerException [Method: CreateReceipt] \nMessage: {0} \n StackTrace: {1}", epiError.Message, epiError.StackTrace));
            }
            catch (Exception x)
            {
                HasErrors = 1;
                Console.WriteLine("SystemException [Method: CreateReceipt] \nMessage: {0} \n StackTrace: {1}", x.Message, x.StackTrace);
                log.WriteOnLogFile(String.Format("SystemException [Method: CreateReceipt] \nMessage: {0} \n StackTrace: {1}", x.Message, x.StackTrace));
            }
            
        }

        private String AddReceiptLine(DataRow PODetail, Int32 VendorNum, String PackSlip, Int32 POrdernum)
        {
            //  Campos en PODetail
            //    [0] - IDParte,          [1] - descripcion,   [2] - CantidadOrden, 
            //    [3] - CantidadRecibida, [4] - linea LineaOC, [5] - UnidadMedida, 
            //    [6] - AlmPrimario,      [7] - UbicPrimaria,  [8] - UbicExcedentes, 
            //    [9] - Area,             [10] - Zona,         [11] - CustID, 
            //    [12] - NomCliente       [13] - Peso          [14] - Volumen
            //    [15] - UdPeso
            try
            {
                String dtlCollector = String.Empty;
                String purPoint = String.Empty;

                string appServerUrl = string.Empty;
                EnvironmentInformation.ConfigurationFileName = fileSys;
                appServerUrl = AppSettingsHandler.AppServerUrl;
                CustomBinding wcfBinding = NetTcp.UsernameWindowsChannel();
                Uri CustSvcUri = new Uri(String.Format("{0}/Erp/BO/{1}.svc", appServerUrl, "Receipt"));

                using (Erp.Proxy.BO.ReceiptImpl BOReceipt = new Erp.Proxy.BO.ReceiptImpl(wcfBinding, CustSvcUri))
                {
                    BOReceipt.ClientCredentials.UserName.UserName = cred.defaultUser;
                    BOReceipt.ClientCredentials.UserName.Password = cred.defaultPass;
                    ReceiptDataSet ds = new ReceiptDataSet();
                    ds.EnforceConstraints = false;
                                        
                    if (Convert.ToDecimal(PODetail[3]) > 0)
                    {
                        String SerialWarning, WarnMsg;
                        BOReceipt.GetNewRcvDtl(ds, VendorNum, purPoint, PackSlip);
                            ds.Tables["RcvDtl"].Rows[0]["ReceiptType"] = "P";
                            ds.Tables["RcvDtl"].Rows[0]["QtyOption"] = "Our";
                            ds.Tables["RcvDtl"].Rows[0]["IUM"] = PODetail[5].ToString();
                            ds.Tables["RcvDtl"].Rows[0]["VendorQty"] = Convert.ToDecimal(PODetail[2]);
                            //ds.Tables["RcvDtl"].Rows[0]["PUM"] = PODetail[5].ToString();
                            ds.Tables["RcvDtl"].Rows[0]["PORelNum"] = 1;
                            ds.Tables["RcvDtl"].Rows[0]["ReceivedTo"] = "PUR-STK";
                            
                        BOReceipt.GetDtlPOLineInfo(ds, VendorNum, purPoint, PackSlip, 0, Convert.ToInt32(PODetail[4]), out SerialWarning);
                            ds.Tables["RcvDtl"].Rows[0]["OurQty"] = Convert.ToDecimal(PODetail[3]);
                            ds.Tables["RcvDtl"].Rows[0]["InputOurQty"] = Convert.ToDecimal(PODetail[3]);

                        BOReceipt.GetDtlQtyInfo(ds, VendorNum, purPoint, PackSlip, 0, Convert.ToDecimal(PODetail[3]), PODetail[5].ToString(), "OurQty", out WarnMsg);

                        ds.Tables["RcvDtl"].Rows[0]["WareHouseCode"] = PODetail[6].ToString();
                        ds.Tables["RcvDtl"].Rows[0]["BinNum"] = PODetail[7].ToString();
                        ds.Tables["RcvDtl"].Rows[0]["Received"] = true;

                        Console.WriteLine($"WarningMsg: {WarnMsg}");

                        BOReceipt.Update(ds);

                        Console.WriteLine(String.Format("Status [OK] - Linea: {0} - Parte: {1} cargada correctamente.", PODetail[4].ToString(), PODetail[0].ToString()));
                        dtlCollector = String.Format("Status [OK] - Linea: {0} - Parte: {1} cargada correctamente.", PODetail[4].ToString(), PODetail[0].ToString());
                    }
                    else
                    {
                        Console.WriteLine(String.Format("Status [ERROR] - Linea: {0} - Parte: {1} no se aplica en recepción porque la cantidad recibida es {2}.", PODetail[4].ToString(), PODetail[0].ToString(), PODetail[3]));
                        dtlCollector = String.Format("Status [ERROR] - Linea: {0} - Parte: {1} no se aplica en recepción porque la cantidad recibida es {2}.", PODetail[4].ToString(), PODetail[0].ToString(), PODetail[3]);
                    }
                }
                
                return dtlCollector;
            }
            catch (Ice.Common.BusinessObjectException epiError)
            {
                Console.WriteLine(String.Format("Status [ERROR] - BusinessObjectException [Method: AddReceiptLine]  \nMessage: {0} \nStackTrace: {1}", epiError.Message, epiError.StackTrace));
                return String.Format("Status [ERROR] - BusinessObjectException [Method: AddReceiptLine]  \nMessage: {0} \nStackTrace: {1}", epiError.Message, epiError.StackTrace);
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Status [ERROR] - SystemException [Method: AddReceiptLine] \nMessage: {0} \nStackTrace{1}", e.Message, e.StackTrace));
                return String.Format("Status [ERROR] - SystemException [Method: AddReceiptLine] \nMessage: {0} \nStackTrace{1}", e.Message, e.StackTrace);
            }
        }

        private Int32 GetVendorNum(String VendorID)
        {
            try
            {
                String connEpicor = String.Format(ConfigurationManager.AppSettings["connERP"].ToString(), "***", "***");
                stmt = new Statements();
                sqlBPM = new SQLOperations(connEpicor);
                DataTable dt = sqlBPM.getRecords(String.Format(stmt.GETEPIVENDORNUM, VendorID));
                Int32 vendor = (dt.Rows.Count > 0) ? Convert.ToInt32(dt.Rows[0][0]) : 0;

                return vendor;
            }
            catch(Exception e)
            {
                Console.WriteLine(String.Format("Error al obtener VendorNum: {0} -> {1}", e.Message, e.StackTrace));
                return 0;
            }
        }
    }
}
