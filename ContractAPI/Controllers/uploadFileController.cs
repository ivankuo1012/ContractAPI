using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace ContractAPI.Controllers
{
    public class uploadFileController : ApiController
    {
        string consString = System.Configuration.ConfigurationManager.AppSettings.Get("ContractDbConnStr");
        public HttpResponseMessage Upload()
        {
            HttpResponseMessage result;
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
                resultAry.Add("file_path", filePath);

                resultAry.Add("import_result", importDB);


                result = Request.CreateResponse(HttpStatusCode.Created, resultAry);


            }
            else
            {

                result = Request.CreateResponse(HttpStatusCode.BadRequest);

            }
            return result;
        }
        private Dictionary<string, dynamic> readExcelToDb(string filename)
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
                    result.Add("row_count", numberOfRecords);
                    return result;
                }
                Debug.WriteLine(dataTable.Rows[0]["TABLE_NAME"].ToString());
                string sheetName = dataTable.Rows[0]["TABLE_NAME"].ToString();
                result.Add("sheetName", sheetName);

                string qs = "select * from[" + sheetName + "]";
                const int columnCnt = 14;
                //string[] contractColumn = new string[columnCnt] { "contract_id", "customer_name", "project_name", "item_name", "start_date", "end_date", "start_date", "end_date", "dept", "sales", "pjm", "contact", "contact_1","warranty" };
                string[] contractColumn = new string[10] { "contract_id", "customer_name", "project_name", "start_date", "end_date", "dept", "sales", "pjm", "contact", "contact_1"};
                string[] itemColumn = new string[5] { "contract_id", "item_name", "start_date", "end_date", "warranty" };
                conn.Open();
                string sSqlInsert = "";
                string sSqlItemInsert = "";
                string SSqlItemDelete = "";
                int rowCntErr = 0;
                var ContractList = new Dictionary<string, string>();
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
                                    if (ColCnt != columnCnt)
                                    {
                                        rowCntErr++;
                                    }
                                    string contractId = dr[0].ToString();



                                    sSqlInsert += "IF EXISTS (SELECT * FROM CONTRACTS WHERE CONTRACT_ID='" + contractId + "' ) update CONTRACTS set ";
                                    SSqlItemDelete += "delete from items where contract_id='" + contractId + "';";

                                    sSqlInsert+= $"{contractColumn[1]} = '{dr[1]}', ";
                                    sSqlInsert+= $"{contractColumn[2]} = '{dr[2]}', ";
                                    sSqlInsert+= (CheckDate(dr[4].ToString()) == null) ? null:$"{contractColumn[3]} = '{CheckDate(dr[4].ToString())}', ";
                                    sSqlInsert+= (CheckDate(dr[5].ToString()) == null) ? null:$"{contractColumn[4]} = '{CheckDate(dr[5].ToString())}', ";
                                    sSqlInsert+= $"{contractColumn[5]} = '{dr[8]}', ";
                                    sSqlInsert+= $"{contractColumn[6]} = '{dr[9]}', ";
                                    sSqlInsert+= $"{contractColumn[7]} = '{dr[10]}', ";
                                    sSqlInsert+= $"{contractColumn[8]} = '{dr[11]}', ";
                                    sSqlInsert+= $"{contractColumn[9]} = '{dr[12]}' ";
                                    sSqlInsert += $"where contract_id= '{contractId}' ";
                                    sSqlInsert += "else insert into contracts(";
                                    for(var i = 0; i < contractColumn.Length; i++)
                                    {
                                        if (i != 0)  sSqlInsert += ","; 
                                        sSqlInsert += $"{contractColumn[i]}";

                                    }
                                    sSqlInsert += ") values (";
                                    sSqlInsert += $"'{dr[0]}',";
                                    sSqlInsert += $"'{dr[1]}',";
                                    sSqlInsert += $"'{dr[2]}',";

                                    sSqlInsert += (CheckDate(dr[4].ToString()) == null) ? "null," : $"'{CheckDate(dr[4].ToString())}',";
                                    sSqlInsert += (CheckDate(dr[5].ToString()) == null) ? "null," : $"'{CheckDate(dr[5].ToString())}',";
                                    sSqlInsert += $"'{dr[8]}',";
                                    sSqlInsert += $"'{dr[9]}',";
                                    sSqlInsert += $"'{dr[10]}',";
                                    sSqlInsert += $"'{dr[11]}',";
                                    sSqlInsert += $"'{dr[12]}'";
                                    sSqlInsert +=");";

                                    sSqlItemInsert += $"INSERT INTO items (contract_id, item_name,start_date, end_date,warranty) values (";
                                    sSqlItemInsert += $"'{dr[0]}',";
                                    sSqlItemInsert += $"'{dr[3]}',";


                                    sSqlItemInsert += (CheckDate(dr[6].ToString()) == null) ? "null," : $"'{CheckDate(dr[4].ToString())}',";
                                    sSqlItemInsert += (CheckDate(dr[7].ToString()) == null) ? "null," : $"'{CheckDate(dr[5].ToString())}',";
                                    sSqlItemInsert += $"'{dr[13]}'";
                                    sSqlItemInsert +=")";
                                    
                                    

                                    

                                }
                                //Debug.WriteLine(sSqlInsert + "\r\n");
                                 Debug.WriteLine(sSqlItemInsert);

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
                if (rowCntErr > 0)
                {
                    result.Add("error", "檔案錯誤");
                    result.Add("row_count", numberOfRecords);
                    return result;
                }
                result.Add("error", "");


                SqlCommand sqlInsert = new SqlCommand(sSqlInsert, conn);
                sqlInsert.ExecuteNonQuery();
                SqlCommand sqlDeleteItem = new SqlCommand(SSqlItemDelete, conn);
                sqlDeleteItem.ExecuteNonQuery();
                SqlCommand sqlInsertItem = new SqlCommand(sSqlItemInsert, conn);

                numberOfRecords += sqlInsertItem.ExecuteNonQuery();
                conn.Close();

                cn.Close();






                result.Add("row_count", numberOfRecords);
            }
            return result;
        }

        private class FileName
        {
            string fileName { get; set; }
        }
        private string CheckDate(string date)
        {
            DateTime temp;
            string dateString;
            if (DateTime.TryParse(date, out temp))
            {
                // Debug.WriteLine(dr[i].ToString());


                temp = Convert.ToDateTime(date);
                dateString = temp.ToString("yyyy/MM/dd");


            }
            else
            {
                //dateString = DateTime.MinValue.ToString("yyyy/MM/dd");
                dateString = null;
            }
            return dateString;
        }
        [HttpPost]
        public HttpResponseMessage importExcelToDb()
        {
            HttpResponseMessage returnResult;
            var httpRequest = HttpContext.Current.Request;

            var result = new Dictionary<string, dynamic>();
            int numberOfRecords = 0;


            var filename = httpRequest["filename"];
        
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
            
            Debug.WriteLine("fileName: "+ filename);
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
                    result.Add("row_count", numberOfRecords);
                    returnResult = Request.CreateResponse(HttpStatusCode.Created,result);

                    return returnResult;
                }
                Debug.WriteLine(dataTable.Rows[0]["TABLE_NAME"].ToString());
                string sheetName = dataTable.Rows[0]["TABLE_NAME"].ToString();
                result.Add("sheetName", sheetName);

                string qs = "select * from[" + sheetName + "]";
                const int columnCnt = 14;
                string[] contractColumn = new string[10] { "contract_id", "customer_name", "project_name", "start_date", "end_date", "dept", "sales", "pjm", "contact", "contact_1" };
                string[] itemColumn = new string[5] { "contract_id", "item_name", "start_date", "end_date", "warranty" };
                conn.Open();
                string sSqlInsert = "";
                string sSqlItemInsert = "";
                string SSqlItemDelete = "";
                int rowCntErr = 0;
                var ContractList = new Dictionary<string, string>();
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
                                    if (ColCnt != columnCnt)
                                    {
                                        rowCntErr++;
                                    }
                                    string contractId = dr[0].ToString();
                                    sSqlInsert += "IF EXISTS (SELECT * FROM CONTRACTS WHERE CONTRACT_ID='" + contractId + "' ) update CONTRACTS set ";
                                    SSqlItemDelete += "delete from items where contract_id='" + contractId + "';";

                                    sSqlInsert += $"{contractColumn[1]} = '{dr[1]}', ";
                                    sSqlInsert += $"{contractColumn[2]} = '{dr[2]}', ";
                                    sSqlInsert += (CheckDate(dr[4].ToString()) == null) ? null : $"{contractColumn[3]} = '{CheckDate(dr[4].ToString())}', ";
                                    sSqlInsert += (CheckDate(dr[5].ToString()) == null) ? null : $"{contractColumn[4]} = '{CheckDate(dr[5].ToString())}', ";
                                    sSqlInsert += $"{contractColumn[5]} = '{dr[8]}', ";
                                    sSqlInsert += $"{contractColumn[6]} = '{dr[9]}', ";
                                    sSqlInsert += $"{contractColumn[7]} = '{dr[10]}', ";
                                    sSqlInsert += $"{contractColumn[8]} = '{dr[11]}', ";
                                    sSqlInsert += $"{contractColumn[9]} = '{dr[12]}' ";
                                    sSqlInsert += $"where contract_id= '{contractId}' ";
                                    sSqlInsert += "else insert into contracts(";
                                    for (var i = 0; i < contractColumn.Length; i++)
                                    {
                                        if (i != 0) sSqlInsert += ",";
                                        sSqlInsert += $"{contractColumn[i]}";

                                    }
                                    sSqlInsert += ") values (";
                                    sSqlInsert += $"'{dr[0]}',";
                                    sSqlInsert += $"'{dr[1]}',";
                                    sSqlInsert += $"'{dr[2]}',";

                                    sSqlInsert += (CheckDate(dr[4].ToString()) == null) ? "null," : $"'{CheckDate(dr[4].ToString())}',";
                                    sSqlInsert += (CheckDate(dr[5].ToString()) == null) ? "null," : $"'{CheckDate(dr[5].ToString())}',";
                                    sSqlInsert += $"'{dr[8]}',";
                                    sSqlInsert += $"'{dr[9]}',";
                                    sSqlInsert += $"'{dr[10]}',";
                                    sSqlInsert += $"'{dr[11]}',";
                                    sSqlInsert += $"'{dr[12]}'";
                                    sSqlInsert += ");";

                                    sSqlItemInsert += $"INSERT INTO items (contract_id, item_name,start_date, end_date,warranty) values (";
                                    sSqlItemInsert += $"'{dr[0]}',";
                                    sSqlItemInsert += $"'{dr[3]}',";


                                    sSqlItemInsert += (CheckDate(dr[6].ToString()) == null) ? "null," : $"'{CheckDate(dr[4].ToString())}',";
                                    sSqlItemInsert += (CheckDate(dr[7].ToString()) == null) ? "null," : $"'{CheckDate(dr[5].ToString())}',";
                                    sSqlItemInsert += $"'{dr[13]}'";
                                    sSqlItemInsert += ")";





                                }
                                Debug.WriteLine(sSqlInsert + "\r\n");
                                // Debug.WriteLine(SSqlItemDelete);

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
                if (rowCntErr > 0)
                {
                    result.Add("error", "檔案錯誤");
                    result.Add("row_count", numberOfRecords);
                    returnResult = Request.CreateResponse(HttpStatusCode.Created, result);

                    return returnResult = Request.CreateResponse(HttpStatusCode.Created, result);
                    ;
                }
                result.Add("error", "");


                SqlCommand sqlInsert = new SqlCommand(sSqlInsert, conn);
                sqlInsert.ExecuteNonQuery();
                SqlCommand sqlDeleteItem = new SqlCommand(SSqlItemDelete, conn);
                sqlDeleteItem.ExecuteNonQuery();
                SqlCommand sqlInsertItem = new SqlCommand(sSqlItemInsert, conn);

                numberOfRecords += sqlInsertItem.ExecuteNonQuery();
                conn.Close();

                cn.Close();






                result.Add("row_count", numberOfRecords);
            }
            returnResult = Request.CreateResponse(HttpStatusCode.Created, result);
            return returnResult;
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
