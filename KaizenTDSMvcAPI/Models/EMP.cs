//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace KaizenTDSMvcAPI.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class EMP
    {
        public int ID { get; set; }
        public string LASTNAME { get; set; }
        public string FIRSTNAME { get; set; }
        public string MIDNAME { get; set; }
        public string SSNO { get; set; }
        public Nullable<System.DateTime> LASTCHANGED { get; set; }
        public short VISITOR { get; set; }
        public short ALLOWEDVISITORS { get; set; }
        public Nullable<int> ASSET_GROUPID { get; set; }
        public int LNL_DBID { get; set; }
        public Nullable<short> GUARD { get; set; }
        public int SEGMENTID { get; set; }
    }
}
