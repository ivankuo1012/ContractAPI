using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;


namespace ContractAPI.Controllers
{
    public class LdapController : ApiController
    {
		public Dictionary<string, string> Login(UserAuthData userdata)
		{
			string username = userdata.Username;
			string password = userdata.Password;
			Dictionary<string, string> userData = new Dictionary<string, string>();

			if (ValidateLDAPUser(username, password) && GetUserDb(username))
			{
				userData = GetLdapUserData(username);
				//return userData;
			}
			return userData;


		}

		static bool ValidateLDAPUser(string username, string password)
		{

			string ldapserver = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServer"); //= "localhost";
			string port = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServerPort"); // = "10389";
			try
			{
				using (var ldapConnection = new LdapConnection(
						new LdapDirectoryIdentifier($"{ldapserver}:{port}")))
				{
					ldapConnection.AuthType = AuthType.Basic;
					ldapConnection.AutoBind = false;
					ldapConnection.Timeout = new TimeSpan(0, 0, 0, 15);
					var ldapUserId = username + "@systex.tw";
					var credential = new NetworkCredential(ldapUserId, password);
					ldapConnection.Bind(credential);


					return true;
				}

			}
			catch (LdapException e)
			{
				Console.WriteLine(("Error with ldap server " + ldapserver + e.ToString()));
				return false;
			}
		}
		[System.Web.Mvc.HttpPost]
		public Dictionary<string, string> GetLdapUserData(string searchUser)
		{
			string ldapserver = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServer"); //= "localhost";
			string port = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServerPort"); // = "10389";
			string path = "LDAP://" + ldapserver + ":" + port + "/DC=systex,DC=tw";
			string username = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServerUserName");//= "1600218s";
			string password = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServerUserPassword"); //= "P@ssw0rdIvankuo";
			string role = System.Configuration.ConfigurationManager.AppSettings.Get("role"); //= "P@ssw0rdIvankuo";
																							 //init a directory entry
			DirectoryEntry dEntry = new DirectoryEntry(path, username, password);
			//dEntry.Path= path;
			DirectorySearcher dSearcher = new DirectorySearcher(dEntry);

			Dictionary<string, string> ldapUserData = new Dictionary<string, string>();

			dSearcher.Filter = "(|(cn=*" + searchUser + "*)(displayname=*" + searchUser + "*)(sn=*" + searchUser + "*))";
			SearchResult result = dSearcher.FindOne();
			string[] listLdapField = { "displayname", "title", "department", "name", "mail" };
			if (result != null)
			{
				// user exists, cycle through LDAP fields (cn, telephonenumber etc.)  

				ResultPropertyCollection fields = result.Properties;

				foreach (String ldapField in fields.PropertyNames)
				{
					// cycle through objects in each field e.g. group membership  
					// (for many fields there will only be one object such as name)  

					foreach (Object myCollection in fields[ldapField])
						if (listLdapField.Contains(ldapField))
						{
							ldapUserData.Add(ldapField, myCollection.ToString());

						}
					//
					//;
					//Console.WriteLine(String.Format("{0,-20} : {1}",ldapField, myCollection.ToString()));
				}
				ldapUserData.Add("login", true.ToString());
				ldapUserData.Add("access_token", "access_token");
				ldapUserData.Add("role", role);
			}

			else
			{
				// user does not exist  
				Console.WriteLine("User not found!");
			}

			return ldapUserData;



		}
		private bool GetUserDb(string name)
        {
			string sDataSource = "10.1.54.236"; // System.Configuration.ConfigurationManager.AppSettings.Get("DataSource");
			string sCatalog = "B110_CONTRACT"; //System.Configuration.ConfigurationManager.AppSettings.Get("Catalog");
			string sDbUser = "contract"; //System.Configuration.ConfigurationManager.AppSettings.Get("DbUser");
			string sDbPassword = "contract1234"; //DecryptStr(System.Configuration.ConfigurationManager.AppSettings.Get("DbPassword"), sEncrKey);

			

			string consString = "data source=" + sDataSource + "; initial catalog = " + sCatalog + "; user id = " + sDbUser + "; password = " + sDbPassword + "";

			SqlConnection conn = new SqlConnection(consString);

			//SqlConnection conn = new SqlConnection("data source=.\\SQLExpress; initial catalog = FUBON_DLP; user id = fubon_dlp; password = 1234");
			conn.Open();
			if ((conn.State & ConnectionState.Open) > 0)
            {
				string sSqlCmdUser = $"select * from users where user_id='1600218s' and user_status=1";
				Debug.WriteLine(sSqlCmdUser);
				//string sSqlCmdUser = "select * from user";
				//Console.WriteLine(sSqlCmdUser);
				SqlCommand cmd = new SqlCommand(sSqlCmdUser, conn);
				SqlDataReader dr = cmd.ExecuteReader();
				if (dr.HasRows)
				{
					return true;
				}
			}
				


			return false;
        }
		public List<Dictionary<string, string>> SearchLdapUserData(string searchUser)
		{
			string ldapserver = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServer"); //= "localhost";
			string port = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServerPort"); // = "10389";
			string path = "LDAP://" + ldapserver + ":" + port + "/DC=systex,DC=tw";
			string username = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServerUserName");//= "1600218s";
			string password = System.Configuration.ConfigurationManager.AppSettings.Get("LdapServerUserPassword"); //= "P@ssw0rdIvankuo";
			string role = System.Configuration.ConfigurationManager.AppSettings.Get("role"); //= "P@ssw0rdIvankuo";
			int i = 0;                                                                                                     //init a directory entry
			DirectoryEntry dEntry = new DirectoryEntry(path, username, password);
			//dEntry.Path= path;
			DirectorySearcher dSearcher = new DirectorySearcher(dEntry);
			List<Dictionary<string, string>> ldapUserDataCollection = new List<Dictionary<string, string>>();
			//Dictionary<int, Dictionary<string, string>> ldapUserData = new Dictionary<int, Dictionary<string, string>>();

			dSearcher.Filter = "(|(cn=*" + searchUser + "*)(displayname=*" + searchUser + "*)(sn=*" + searchUser + "*))";
			SearchResultCollection results = dSearcher.FindAll();
			string[] listLdapField = { "displayname", "title", "department", "name", "mail" };
			if (results != null)
			{
				// user exists, cycle through LDAP fields (cn, telephonenumber etc.)  
				foreach (SearchResult result in results)
				{
					ResultPropertyCollection fields = result.Properties;
					Dictionary<string, string> ldapUserData = new Dictionary<string, string>();
					foreach (String ldapField in fields.PropertyNames)
					{
						// cycle through objects in each field e.g. group membership  
						// (for many fields there will only be one object such as name)  

						foreach (Object myCollection in fields[ldapField])
							if (listLdapField.Contains(ldapField))
							{


								ldapUserData.Add(ldapField, myCollection.ToString());
							}

						//
						//;
						//Console.WriteLine(String.Format("{0,-20} : {1}",ldapField, myCollection.ToString()));
					}
					ldapUserDataCollection.Add(ldapUserData);

					i++;
				}


			}

			else
			{
				// user does not exist  
				Console.WriteLine("User not found!");
			}

			return ldapUserDataCollection;



		}
		[System.Web.Http.HttpPost]
		public string TestApi()
		{
			string test = "test";
			return test;
		}
		[System.Web.Http.HttpPost]
		public IHttpActionResult GetUserData(UserSearchData searchUser)
		{
			//string searchUser = "1600218s";
			Dictionary<string, string> userData = GetLdapUserData(searchUser.searchName);
			//return userData;

			return Ok(userData);


		}
		[System.Web.Http.HttpPost]
		public IHttpActionResult SearchUserData(UserSearchData searchUser)
		{
			//string searchUser = "1600218s";
			List<Dictionary<string, string>> userData = SearchLdapUserData(searchUser.searchName);
			//return userData;

			return Ok(userData);


		}
		
		//[System.Web.Http.HttpPost]
		//public IHttpActionResult GetUserDb(UserSearchData searchUser)
		//{
		//	//string searchUser = "1600218s";
		//	GetUserDb(searchUser);
		//	//return userData;

		//	return Ok(userData);


		//}
		public class UserAuthData
		{
			public string Username { get; set; }
			public string Password { get; set; }
		}
		public class UserData
		{
			public string user_id { get; set; }
			public string user_role { get; set; }
			public string user_status { get; set; }
		}
		public class UserSearchData
		{
			public string searchName { get; set; }
		}
	}
}
