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
                const int columnCnt = 13;
                string[] contractColumn = new string[columnCnt] { "contract_id", "customer_name", "project_name", "item_name", "start_date", "end_date", "start_date", "end_date", "dept", "sales", "pjm", "contact", "contact_1" };
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
                                    for (var i = 0; i < ColCnt; i++)
                                    {
                                        if (i != 3 && i != 6 && i != 7)
                                        {
                                            if (i == 4 || i == 5 )
                                            {
                                                // DateTime date = new DateTime();  
                                                DateTime temp;
                                                string dateString;
                                                if (DateTime.TryParse(dr[i].ToString(), out temp))
                                                {
                                                    // Debug.WriteLine(dr[i].ToString());


                                                    temp = Convert.ToDateTime(dr[i].ToString());
                                                    dateString = temp.ToString("yyyy/MM/dd");


                                                }
                                                else
                                                {
                                                    dateString = DateTime.MinValue.ToString("yyyy/MM/dd");
                                                }
                                                sSqlInsert += contractColumn[i] + "=" + $"'{dateString}'";
                                            }
                                            else
                                            {
                                                sSqlInsert += contractColumn[i] + "=" + $"'{dr[i].ToString()}'";
                                            }
                                             
                                            if (i != ColCnt - 1)
                                            {
                                                sSqlInsert += ",";
                                            }
                                        }


                                    }
                                    //Debug.WriteLine(dr[0].ToString() + "\t" + dr[1].ToString() + "\t" + dr[2].ToString());
                                    sSqlInsert += " where contract_id='" + contractId + "' ";
                                    sSqlInsert += "else  " +
                                                    "INSERT INTO CONTRACTS (";
                                    sSqlItemInsert += "INSERT INTO ITEMS(contract_id," + contractColumn[3] + "," + contractColumn[6] + "," + contractColumn[7] + "";
                                    for (var i = 0; i < ColCnt; i++)
                                    {
                                        if (i != 3 && i != 6 && i != 7)
                                        {
                                            sSqlInsert += $"{contractColumn[i]}";
                                            if (i != ColCnt - 1)
                                            {
                                                sSqlInsert += ",";
                                            }
                                        }

                                    }

                                    sSqlInsert += ")values(";
                                    sSqlItemInsert += ")values('" + contractId + "',";
                                    Debug.WriteLine(contractId + "\r\n");
                                    for (var i = 0; i < ColCnt; i++)
                                    {
                                        if (i == 4 || i == 5 || i == 6 || i == 7)
                                        {
                                            // DateTime date = new DateTime();  
                                            DateTime temp;
                                            string dateString;
                                            if (DateTime.TryParse(dr[i].ToString(), out temp))
                                            {
                                                // Debug.WriteLine(dr[i].ToString());


                                                temp = Convert.ToDateTime(dr[i].ToString());
                                                dateString = temp.ToString("yyyy/MM/dd");


                                            }
                                            else
                                            {
                                                dateString = DateTime.MinValue.ToString("yyyy/MM/dd");
                                            }
                                            if (i == 4 || i == 5)
                                            {
                                                sSqlInsert += $"'{dateString}'";
                                                if (i != ColCnt - 1)
                                                {
                                                    sSqlInsert += ",";
                                                }
                                            }
                                            if (i == 6 || i == 7)
                                            {
                                                sSqlItemInsert += $"'{dateString}'";
                                                if (i != 7)
                                                {
                                                    sSqlItemInsert += ",";
                                                }
                                            }


                                        }
                                        else
                                        {
                                            if (i != 3 && i != 6 && i != 7)
                                            {
                                                sSqlInsert += $"'{dr[i].ToString().Trim()}'";
                                                if (i != ColCnt - 1)
                                                {
                                                    sSqlInsert += ",";
                                                }
                                            }
                                            else
                                            {

                                                sSqlItemInsert += $"'{dr[i].ToString().Trim()}'";
                                                if (i != 7)
                                                {
                                                    sSqlItemInsert += ",";
                                                }
                                            }
                                        }


                                    }
                                    sSqlInsert += ");\r\n";
                                    sSqlItemInsert += ");";
                                   


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
