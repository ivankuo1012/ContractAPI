using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Diagnostics;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Data.SqlClient;
using System.Data;
using System.Data.OleDb;

namespace ContractAPI.Controllers
{
    public class uploadFileController : ApiController
    {
        string consString = System.Configuration.ConfigurationManager.AppSettings.Get("ContractDbConnStr");
        public HttpResponseMessage Upload()
        {
            HttpResponseMessage result ;
            var httpRequest = HttpContext.Current.Request;
            
            Debug.WriteLine("upload start");
            var resultAry = new Dictionary<string, dynamic>();

            if (httpRequest.Files.Count > 0)
            {
                var docfiles = new List<string>();
                var postedFile = httpRequest.Files[0];
                Debug.WriteLine(httpRequest.Files[0].FileName);
                var filePath = HttpContext.Current.Server.MapPath("~/FileUploads/" + postedFile.FileName);
                postedFile.SaveAs(filePath);
                
                //foreach (string file in httpRequest.Files)
                //{
                //    var postedFile = httpRequest.Files[file]
                //    //Debug.Write(httpRequest.Files[file]);
                //    var filePath = HttpContext.Current.Server.MapPath("~/FileUploads/" + postedFile.FileName);

                //    postedFile.SaveAs(filePath);
                //    docfiles.Add(filePath);
                //}

                Dictionary<string, dynamic> importDB = readExcelToDb(filePath);
                resultAry.Add("file_name", postedFile.FileName);
                resultAry.Add("import_result", importDB);

                
                result = Request.CreateResponse(HttpStatusCode.Created, resultAry);
                

            }
            else
            {
                
                result = Request.CreateResponse(HttpStatusCode.BadRequest);
                
            }
            return result;
        }
        private Dictionary<string,dynamic> readExcelToDb(string filename)
        {
            var result = new Dictionary<string, dynamic>();
            int numberOfRecords = 0;

            SqlConnection conn = new SqlConnection(this.consString);
            //OleDbConnection objConn;
            
            //2.提供者名稱  Microsoft.Jet.OLEDB.4.0適用於2003以前版本，Microsoft.ACE.OLEDB.12.0 適用於2007以後的版本處理 xlsx 檔案
            string ProviderName = "Microsoft.ACE.OLEDB.12.0;";
            //3.Excel版本，Excel 8.0 針對Excel2000及以上版本，Excel5.0 針對Excel97。
             string ExtendedString = "'Excel 8.0;";
            //4.第一行是否為標題
            string Hdr = "Yes;";
            //5.IMEX=1 通知驅動程序始終將「互混」數據列作為文本讀取
             string IMEX = "0';";

            //SqlConnection conn = new SqlConnection("data source=.\\SQLExpress; initial catalog = FUBON_DLP; user id = fubon_dlp; password = 1234");

            string cs =
               "Data Source=" + filename + ";" +
               "Provider=" + ProviderName +
               "Extended Properties=" + ExtendedString +
               "HDR=" + Hdr +
               "IMEX=" + IMEX;
            using (OleDbConnection cn = new OleDbConnection(cs))
            {


                cn.Open();
                DataTable dataTable = cn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                if (dataTable == null)
                {
                    result.Add("error", "檔案無法讀取");
                    return result;
                }
                Debug.WriteLine(dataTable.Rows[0]["TABLE_NAME"].ToString());
                string sheetName = dataTable.Rows[0]["TABLE_NAME"].ToString();
                result.Add("sheetName", sheetName);
               
                string qs = "select * from[" + sheetName + "]";
                string[] contractColumn = new string[11] { "contract_id", "bu", "customer_name", "project_name", "sales_dept", "sales", "start_date", "end_date", "war_end_date", "product_type", "money" };
                conn.Open();
                if ((conn.State & ConnectionState.Open) > 0)
                {
                    try
                    {
                        using (OleDbCommand cmd = new OleDbCommand(qs, cn))
                        {
                            using (OleDbDataReader dr = cmd.ExecuteReader())
                            {

                                while (dr.Read())

                                {

                                    int ColCnt = dr.FieldCount;

                                    string contractId = dr[0].ToString();
                                    string sSqlInsert = "IF EXISTS (SELECT * FROM CONTRACT WHERE CONTRACT_ID='" + contractId + "' )" +
                           " update contract set ";
                                    for (var i = 0; i < ColCnt; i++)
                                    {

                                        sSqlInsert += contractColumn[i] + "=" + $"'{dr[i].ToString()}'";
                                        if (i != ColCnt - 1)
                                        {
                                            sSqlInsert += ",";
                                        }
                                    }
                                    //Debug.WriteLine(dr[0].ToString() + "\t" + dr[1].ToString() + "\t" + dr[2].ToString());
                                    sSqlInsert += " where contract_id='" + contractId + "' ";
                                    sSqlInsert += "else  " +
                            "INSERT INTO contract (contract_id, bu, customer_name, project_name, sales_dept, sales, start_date, end_date, war_end_date, product_type, money)values(";
                                    for (var i = 0; i < ColCnt; i++)
                                    {
                                        sSqlInsert += $"'{dr[i].ToString()}'";
                                        if (i != ColCnt - 1)
                                        {
                                            sSqlInsert += ",";
                                        }
                                    }
                                    sSqlInsert += ")";
                                    //Debug.WriteLine(sSqlInsert);
                                    SqlCommand sqlInsert = new SqlCommand(sSqlInsert, conn);
                                    numberOfRecords += sqlInsert.ExecuteNonQuery();

                                }

                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }


                }
                //foreach (DataRow row in dataTable.Rows)
                //{
                //    // Write the sheet name to the screen

                //    //就是在這取得Sheet Name
                //    Debug.WriteLine("sheetName: " + row["TABLE_NAME"].ToString());

                //}

                conn.Close();
              
                cn.Close();


                



                result.Add("row_count", numberOfRecords);
            }
            return result;
        }
        public class contractData
        {
            public string contract_id { get; set; }
            public string bu { get; set; }
            public string customer_name { get; set; }
            public string project_name { get; set; }
            public string sales_dept { get; set; }
            public string sales { get; set; }
            public string start_date { get; set; }
            public string end_date { get; set; }
            public int money { get; set; }
            public string war_end_date { get; set; }
            public string product_type { get; set; }
        }
    }
}
