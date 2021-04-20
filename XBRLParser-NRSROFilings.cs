//  Copyright 2021, The CFD Blog 

using System;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace XBRLParserForNRSROFilings
{
    class Program
    {
        static void Main()
        {
            string directory = @"X:\ROCRA\SP\Sovereign";

            var directoryArray = Directory.GetFiles(directory, "*.xml")
                .OrderBy(f => new FileInfo(f).Length);

            int fileCount = 0;
            int totalFiles = directoryArray.Count();

            string connectionString = "Data source=(local); Initial Catalog=ROCRA; Integrated Security=SSPI";
            string obligorTable = "dbo.ObligorRatingData";
            string issuerTable = "dbo.IssuerRatingData";

            foreach (var fileNameXml in directoryArray)
            {
                fileCount++;
                Console.SetCursorPosition(0, 0);
                Console.WriteLine($"Now processing file {fileCount} of {totalFiles} in ({directory})");
                XBRLEngine.ReadXML(fileNameXml, connectionString, obligorTable, issuerTable);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    public class XBRLEngine
    {
        public static void ReadXML(string fileNameXml, string connectionString,
            string obligorTable, string issuerTable)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileNameXml);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsmgr.AddNamespace("r", "http://xbrl.sec.gov/ratings/2015-03-31");
            nsmgr.AddNamespace("link", "http://www.xbrl.org/2003/linkbase'");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
            nsmgr.AddNamespace("xl", "http://www.xbrl.org/2003/xl");
            nsmgr.AddNamespace("xbrli", "http://www.xbrl.org/2003/instance");
            nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

            int issuerCount = xmlDoc.SelectNodes("//xbrli:xbrl/r:ROCRA/r:ISD", nsmgr).Count;
            int instrumentCount;
            int ratingsCount;

            DataTable table;

            //  1-pass variables 
            string RAN = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:RAN", nsmgr).InnerText;
            string FCD = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:FCD", nsmgr).InnerText;

            if (issuerCount == 0)
            {
                //  Proceed with obligor type
                int obligorCount = xmlDoc.SelectNodes("//xbrli:xbrl/r:ROCRA/r:OD", nsmgr).Count;

                table = Obligor.GenerateObligorTable();

                for (int i = 0; i < obligorCount; i++)
                {
                    ratingsCount = xmlDoc.SelectNodes($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD", nsmgr).Count;

                    for (int j = 0; j < ratingsCount; j++)
                    {
                        Obligor obligor = new Obligor();

                        //  ROCRA (Record of Credit Rating Actions)
                        obligor.FILENAMEXML = fileNameXml;
                        obligor.RAN = RAN;
                        obligor.FCD = FCD;

                        //  OD (Obligor Details)
                        obligor.OSC = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:OSC", nsmgr).InnerText;

                        try
                        {
                            obligor.OIG = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:OIG", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.OIG = string.Empty;
                        }

                        obligor.OBNAME = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:OBNAME", nsmgr).InnerText;

                        try
                        {
                            obligor.LEI = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:LEI", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.LEI = string.Empty;
                        }

                        try
                        {
                            obligor.CIK = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:CIK", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.CIK = string.Empty;
                        }

                        try
                        {
                            obligor.OI = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:OI", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.OI = string.Empty;
                        }

                        try
                        {
                            obligor.OIS = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:OIS", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.OIS = string.Empty;
                        }

                        try
                        {
                            obligor.OIOS = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:OIOS", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.OIOS = string.Empty;
                        }

                        //  ORD (Obligor Rating Details) 
                        obligor.IP = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:IP", nsmgr).InnerText;

                        obligor.R = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:R", nsmgr).InnerText;

                        obligor.RAD = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:RAD", nsmgr).InnerText;

                        try
                        {
                            obligor.RAC = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:RAC", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.RAC = string.Empty;
                        }

                        try
                        {
                            obligor.WST = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:WST", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.WST = string.Empty;
                        }

                        try
                        {
                            obligor.ROL = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:ROL", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.ROL = string.Empty;
                        }

                        try
                        {
                            obligor.OAN = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:OAN", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.OAN = string.Empty;
                        }

                        try
                        {
                            obligor.RT = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:RT", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.RT = string.Empty;
                        }

                        try
                        {
                            obligor.RST = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:RST", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.RST = string.Empty;
                        }

                        try
                        {
                            obligor.RTT = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:OD[{i + 1}]/r:ORD[{j + 1}]/r:RTT", nsmgr).InnerText;
                        }
                        catch
                        {
                            obligor.RTT = string.Empty;
                        }

                        table.Rows.Add(obligor.FILENAMEXML, obligor.RAN, obligor.FCD,
                            obligor.OSC, obligor.OIG, obligor.OBNAME, obligor.LEI,
                            obligor.CIK, obligor.OI, obligor.OIS, obligor.OIOS,
                            obligor.IP, obligor.R, obligor.RAD, obligor.RAC, obligor.WST,
                            obligor.ROL, obligor.OAN, obligor.RT, obligor.RST, obligor.RTT);
                    }
                }
                SQLInterface.CopyToSql(connectionString, table, obligorTable);
            }
            else
            {
                //  Proceed with issuer type
                table = Issuer.GenerateIssuerTable();

                for (int i = 0; i < issuerCount; i++)
                {
                    instrumentCount = xmlDoc.SelectNodes($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND", nsmgr).Count;

                    for (int j = 0; j < instrumentCount; j++)
                    {
                        ratingsCount = xmlDoc.SelectNodes($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD", nsmgr).Count;

                        for (int k = 0; k < ratingsCount; k++)
                        {
                            Issuer issuer = new Issuer();

                            //  ROCRA (Record of Credit Rating Actions)
                            issuer.FILENAMEXML = fileNameXml;
                            issuer.RAN = RAN;
                            issuer.FCD = FCD;

                            //  ISD (Issuer Detail)
                            issuer.SSC = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:SSC", nsmgr).InnerText;

                            try
                            {
                                issuer.IG = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IG", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.IG = string.Empty;
                            }

                            issuer.ISSNAME = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:ISSNAME", nsmgr).InnerText;

                            try
                            {
                                issuer.LEI = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:LEI", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.LEI = string.Empty;
                            }

                            try
                            {
                                issuer.CIK = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:CIK", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.CIK = string.Empty;
                            }

                            try
                            {
                                issuer.ISI = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:ISI", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.ISI = string.Empty;
                            }

                            try
                            {
                                issuer.ISIS = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:ISIS", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.ISIS = string.Empty;
                            }

                            try
                            {
                                issuer.ISIOS = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:ISIOS", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.ISIOS = string.Empty;
                            }

                            //  IND (Instrument Detail)
                            issuer.OBT = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:OBT", nsmgr).InnerText;

                            issuer.INSTNAME = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INSTNAME", nsmgr).InnerText;

                            try
                            {
                                issuer.CUSIP = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:CUSIP", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.CUSIP = string.Empty;
                            }

                            try
                            {
                                issuer.INI = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INI", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.INI = string.Empty;
                            }

                            try
                            {
                                issuer.INIS = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INIS", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.INIS = string.Empty;
                            }

                            try
                            {
                                issuer.INIOS = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INIOS", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.INIOS = string.Empty;
                            }

                            try
                            {
                                issuer.IRTD = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:IRTD", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.IRTD = string.Empty;
                            }

                            try
                            {
                                issuer.CR = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:CR", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.CR = string.Empty;
                            }

                            try
                            {
                                issuer.MD = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:MD", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.MD = string.Empty;
                            }

                            try
                            {
                                issuer.PV = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:PV", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.PV = string.Empty;
                            }

                            try
                            {
                                issuer.ISUD = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:ISUD", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.ISUD = string.Empty;
                            }

                            try
                            {
                                issuer.RODC = xmlDoc.SelectSingleNode($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:RODC", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.RODC = string.Empty;
                            }

                            //  INRD (Instrument Rating Details)
                            issuer.IP = xmlDoc.SelectSingleNode
                                ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:IP", nsmgr).InnerText;

                            issuer.R = xmlDoc.SelectSingleNode
                                ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:R", nsmgr).InnerText;

                            issuer.RAD = xmlDoc.SelectSingleNode
                                ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:RAD", nsmgr).InnerText;

                            try
                            {
                                issuer.RAC = xmlDoc.SelectSingleNode
                                    ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:RAC", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.RAC = string.Empty;
                            }

                            try
                            {
                                issuer.WST = xmlDoc.SelectSingleNode
                                    ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:WST", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.WST = string.Empty;
                            }

                            try
                            {
                                issuer.ROL = xmlDoc.SelectSingleNode
                                    ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:ROL", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.ROL = string.Empty;
                            }

                            issuer.OAN = xmlDoc.SelectSingleNode
                                    ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:OAN", nsmgr).InnerText;

                            try
                            {
                                issuer.RT = xmlDoc.SelectSingleNode
                                    ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:RT", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.RT = string.Empty;
                            }

                            try
                            {
                                issuer.RST = xmlDoc.SelectSingleNode
                                    ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:RST", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.RST = string.Empty;
                            }

                            try
                            {
                                issuer.RTT = xmlDoc.SelectSingleNode
                                    ($"//xbrli:xbrl/r:ROCRA/r:ISD[{i + 1}]/r:IND[{j + 1}]/r:INRD[{k + 1}]/r:RTT", nsmgr).InnerText;
                            }
                            catch
                            {
                                issuer.RTT = string.Empty;
                            }

                            table.Rows.Add(issuer.FILENAMEXML, issuer.RAN, issuer.FCD,
                                issuer.SSC, issuer.IG, issuer.ISSNAME, issuer.LEI,
                                issuer.CIK, issuer.ISI, issuer.ISIS, issuer.ISIOS,
                                issuer.OBT, issuer.INSTNAME, issuer.CUSIP, issuer.INI,
                                issuer.INIS, issuer.INIOS, issuer.IRTD, issuer.CR,
                                issuer.MD, issuer.PV, issuer.ISUD, issuer.RODC,
                                issuer.IP, issuer.R, issuer.RAD, issuer.RAC, issuer.WST,
                                issuer.ROL, issuer.OAN, issuer.RT, issuer.RST, issuer.RTT);
                        }
                    }
                }
                SQLInterface.CopyToSql(connectionString, table, issuerTable);
            }
        }
    }

    public class SQLInterface
    {
        public static void CopyToSql(string connectionString, DataTable sourceTable, string destinationTable)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connectionString))
            {
                //  Align source and destination columns
                foreach (DataColumn col in sourceTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(col.ColumnName, col.ColumnName));
                }

                //  Buffer table loadings
                bulkCopy.BatchSize = 5000;
                bulkCopy.DestinationTableName = $"{destinationTable}";
                bulkCopy.BulkCopyTimeout = 0;

                try
                {
                    bulkCopy.WriteToServer(sourceTable);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }

    public class Obligor
    {
        public string FILENAMEXML { get; set; }
        public string RAN { get; set; }
        public string FCD { get; set; }
        public string OSC { get; set; }
        public string OIG { get; set; }
        public string OBNAME { get; set; }
        public string LEI { get; set; }
        public string CIK { get; set; }
        public string OI { get; set; }
        public string OIS { get; set; }
        public string OIOS { get; set; }
        public string IP { get; set; }
        public string R { get; set; }
        public string RAD { get; set; }
        public string RAC { get; set; }
        public string WST { get; set; }
        public string ROL { get; set; }
        public string OAN { get; set; }
        public string RT { get; set; }
        public string RST { get; set; }
        public string RTT { get; set; }

        public static DataTable GenerateObligorTable()
        {
            DataTable obligorTable = new DataTable();

            obligorTable.Columns.Add("FILENAMEXML", typeof(string));
            obligorTable.Columns.Add("RAN", typeof(string));
            obligorTable.Columns.Add("FCD", typeof(string));
            obligorTable.Columns.Add("OSC", typeof(string));
            obligorTable.Columns.Add("OIG", typeof(string));
            obligorTable.Columns.Add("OBNAME", typeof(string));
            obligorTable.Columns.Add("LEI", typeof(string));
            obligorTable.Columns.Add("CIK", typeof(string));
            obligorTable.Columns.Add("OI", typeof(string));
            obligorTable.Columns.Add("OIS", typeof(string));
            obligorTable.Columns.Add("OIOS", typeof(string));
            obligorTable.Columns.Add("IP", typeof(string));
            obligorTable.Columns.Add("R", typeof(string));
            obligorTable.Columns.Add("RAD", typeof(string));
            obligorTable.Columns.Add("RAC", typeof(string));
            obligorTable.Columns.Add("WST", typeof(string));
            obligorTable.Columns.Add("ROL", typeof(string));
            obligorTable.Columns.Add("OAN", typeof(string));
            obligorTable.Columns.Add("RT", typeof(string));
            obligorTable.Columns.Add("RST", typeof(string));
            obligorTable.Columns.Add("RTT", typeof(string));

            return obligorTable;
        }
    }

    public class Issuer
    {
        public string FILENAMEXML { get; set; }
        public string RAN { get; set; }
        public string FCD { get; set; }
        public string SSC { get; set; }
        public string IG { get; set; }
        public string ISSNAME { get; set; }
        public string LEI { get; set; }
        public string CIK { get; set; }
        public string ISI { get; set; }
        public string ISIS { get; set; }
        public string ISIOS { get; set; }
        public string OBT { get; set; }
        public string INSTNAME { get; set; }
        public string CUSIP { get; set; }
        public string INI { get; set; }
        public string INIS { get; set; }
        public string INIOS { get; set; }
        public string IRTD { get; set; }
        public string CR { get; set; }
        public string MD { get; set; }
        public string PV { get; set; }
        public string ISUD { get; set; }
        public string RODC { get; set; }
        public string IP { get; set; }
        public string R { get; set; }
        public string RAD { get; set; }
        public string RAC { get; set; }
        public string WST { get; set; }
        public string ROL { get; set; }
        public string OAN { get; set; }
        public string RT { get; set; }
        public string RST { get; set; }
        public string RTT { get; set; }

        public static DataTable GenerateIssuerTable()
        {
            DataTable issuerTable = new DataTable();

            issuerTable.Columns.Add("FILENAMEXML", typeof(string));
            issuerTable.Columns.Add("RAN", typeof(string));
            issuerTable.Columns.Add("FCD", typeof(string));
            issuerTable.Columns.Add("SSC", typeof(string));
            issuerTable.Columns.Add("IG", typeof(string));
            issuerTable.Columns.Add("ISSNAME", typeof(string));
            issuerTable.Columns.Add("LEI", typeof(string));
            issuerTable.Columns.Add("CIK", typeof(string));
            issuerTable.Columns.Add("ISI", typeof(string));
            issuerTable.Columns.Add("ISIS", typeof(string));
            issuerTable.Columns.Add("ISIOS", typeof(string));
            issuerTable.Columns.Add("OBT", typeof(string));
            issuerTable.Columns.Add("INSTNAME", typeof(string));
            issuerTable.Columns.Add("CUSIP", typeof(string));
            issuerTable.Columns.Add("INI", typeof(string));
            issuerTable.Columns.Add("INIS", typeof(string));
            issuerTable.Columns.Add("INIOS", typeof(string));
            issuerTable.Columns.Add("IRTD", typeof(string));
            issuerTable.Columns.Add("CR", typeof(string));
            issuerTable.Columns.Add("MD", typeof(string));
            issuerTable.Columns.Add("PV", typeof(string));
            issuerTable.Columns.Add("ISUD", typeof(string));
            issuerTable.Columns.Add("RODC", typeof(string));
            issuerTable.Columns.Add("IP", typeof(string));
            issuerTable.Columns.Add("R", typeof(string));
            issuerTable.Columns.Add("RAD", typeof(string));
            issuerTable.Columns.Add("RAC", typeof(string));
            issuerTable.Columns.Add("WST", typeof(string));
            issuerTable.Columns.Add("ROL", typeof(string));
            issuerTable.Columns.Add("OAN", typeof(string));
            issuerTable.Columns.Add("RT", typeof(string));
            issuerTable.Columns.Add("RST", typeof(string));
            issuerTable.Columns.Add("RTT", typeof(string));

            return issuerTable;
        }
    }
}