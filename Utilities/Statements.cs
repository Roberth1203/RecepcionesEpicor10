using System;

namespace Utilities
{
    public class Statements
    {
        public String GETFOLIOSVERIFICADOS = "SELECT TOP 1 id idBPM, idFolio, IDProveedor, NumeroOC,Nombre FROM CAOR.PLANEADOS WHERE statusProceso = 'RECIBIDO' AND FechaSalidaProveedor IS NOT NULL AND STATUSINTERFAZEPICOR IS NULL;";
        public String GETDETALLEOC = "SELECT a.IDParte, a.descripcion, a.CantidadOrden, a.CantidadRecibida, a.linea LineaOC, a.UnidadMedida, a.AlmPrimario, a.UbicPrimaria, a.UbicExcedentes, a.Area, a.Zona, a.CustID, a.NomCliente, a.idDetalles, a.Peso, a.UdPeso, a.Volumen,a.idDetalles FROM CAOR.DETALLES a WHERE a.idFolio = {0} AND a.NumeroOC = {1};";
        public String GETEPIVENDORNUM = "SELECT VendorNum FROM ERP10DB.dbo.Vendor WHERE Company = 'DLMAC' AND VendorID = '{0}';";
        public String GETPORDERINFO = "SELECT CAST(UnitCost AS DECIMAL(10,2)) UnitCost FROM dbo.PODetail WHERE Company = 'DLMAC' AND PONUM = {0} AND POLine = {1};";
        public String GETCOSTOSPESOSOC = "SELECT a.UnitCost, a.DocUnitCost, a.LineDesc, b.GrossWeight, b.GrossWeightUOM FROM ERP10DB.dbo.PODetail a INNER JOIN ERP10DB.dbo.Part b ON (b.Company = a.Company AND b.PartNum = a.PartNum) WHERE a.Company = 'DLMAC' AND a.PONUM = {0} AND a.POLine = {1};";

        public String UPDIDPLANEADOS = "UPDATE CAOR.PLANEADOS SET STATUSINTERFAZEPICOR = {0} WHERE id IN({1});";
        public String UPDSTATUSDETALLES = "UPDATE CAOR.DETALLES SET STATUSINTERFAZEPICOR = {0}, FECHACIERREINTERFAZ = GETDATE() WHERE idDetalles = {1};";
    }
}
