//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace KSUCorner.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Group
    {
        public int GroupID { get; set; }
        public string Name { get; set; }
        public int AccountID { get; set; }
        public string ImagePath { get; set; }
        public string ImageLinkType { get; set; }
        public bool IsPublic { get; set; }
        public string Description { get; set; }
        public string Mission { get; set; }
        public long Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public System.DateTime CreateDate { get; set; }
        public System.DateTime LastUpdateDate { get; set; }
    }
}