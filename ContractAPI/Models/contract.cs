//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace ContractAPI.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class contract
    {
        public string contract_id { get; set; }
        public string bu { get; set; }
        public string customer_name { get; set; }
        public string project_name { get; set; }
        public string sales_dept { get; set; }
        public string sales { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public Nullable<int> money { get; set; }
        public string war_end_date { get; set; }
        public string product_type { get; set; }
    }
}