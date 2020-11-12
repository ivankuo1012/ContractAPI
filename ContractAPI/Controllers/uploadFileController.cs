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
                //    var postedFile = httpRequest.Files[file];
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
            SqlConnection conn = new SqlConnection(this.consString);



            //SqlConnection conn = new SqlConnection("data source=.\\SQLExpress; initial catalog = FUBON_DLP; user id = fubon_dlp; password = 1234");
            

            
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            Excel.Range range;
            string str;
            int rCnt;
            int cCnt;
            int rw = 0;
            int cl = 0;

            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Open(@filename, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            range = xlWorkSheet.UsedRange;
            rw = range.Rows.Count;
            cl = range.Columns.Count;
            int numberOfRecords = 0;
            var result = new Dictionary<string, dynamic>();
            if (cl != 11)
            {
                result.Add("error", "檔案格式錯誤");
                return result;
            }
            string[] contractColumn = new string[11] { "contract_id", "bu", "customer_name", "project_name", "sales_dept", "sales", "start_date", "end_date", "war_end_date", "product_type", "money" };
            conn.Open();
            if ((conn.State & ConnectionState.Open) > 0)    
            {
                for (rCnt = 2; rCnt <= rw-1; rCnt++)
                {
                    string contractId = (string)(range.Cells[rCnt, 1] as Excel.Range).Text;
                    Debug.WriteLine(contractId);
                    string sSqlInsert = "IF EXISTS (SELECT * FROM CONTRACT WHERE CONTRACT_ID='" + contractId + "' )" +
                        "update contract set ";
                    for (cCnt = 1; cCnt <= cl; cCnt++)
                    {
                        if (cCnt == cl)
                        {
                            int money = (int)(range.Cells[rCnt, cCnt] as Excel.Range).Value2;

                            sSqlInsert += $"money='{money}'";
                        }
                        else
                        {
                            str = (string)(range.Cells[rCnt, cCnt] as Excel.Range).Text;

                            sSqlInsert += contractColumn[cCnt-1] + "=" + $"'{str}',";
                        }

                        //Debug.WriteLine(sSqlInsert);

                    }
                    sSqlInsert += " where contract_id='"+contractId+"' ";
                    sSqlInsert += "else  " +
                        "INSERT INTO contract (contract_id, bu, customer_name, project_name, sales_dept, sales, start_date, end_date, war_end_date, product_type, money)values(";
                    for (cCnt = 1; cCnt <= cl; cCnt++)
                    {
                        if (cCnt == cl)
                        {
                           int money = (int)(range.Cells[rCnt, cCnt] as Excel.Range).Value2;
                            
                            sSqlInsert += $"'{money}'";
                        }
                        else
                        {
                            str = (string)(range.Cells[rCnt, cCnt] as Excel.Range).Text;
                            
                            sSqlInsert += $"'{str}',";
                        }
                        
                        //Debug.WriteLine(sSqlInsert);
                        
                    }
                    sSqlInsert += ")";
                    Debug.WriteLine(sSqlInsert);

                    SqlCommand sqlInsert = new SqlCommand(sSqlInsert, conn);
                    numberOfRecords += sqlInsert.ExecuteNonQuery();
                }

                xlWorkBook.Close(true, null, null);
                xlApp.Quit();

                
                
                //string sSqlCmdUser = "select * from user";  
                //Console.WriteLine(sSqlCmdUser);
               

                
                
            }
            conn.Close();

            Marshal.ReleaseComObject(xlWorkSheet);
            Marshal.ReleaseComObject(xlWorkBook);
            Marshal.ReleaseComObject(xlApp);
            result.Add("row_count", numberOfRecords);
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
