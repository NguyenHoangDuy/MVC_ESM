﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Model
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class DKMHEntities : DbContext
    {
        public DKMHEntities()
            : base("name=DKMHEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<bomon> bomons { get; set; }
        public DbSet<CaThi> CaThis { get; set; }
        public DbSet<chuyennganh> chuyennganhs { get; set; }
        public DbSet<giaovien> giaoviens { get; set; }
        public DbSet<khoa> khoas { get; set; }
        public DbSet<khoi> khois { get; set; }
        public DbSet<lichhocvu> lichhocvus { get; set; }
        public DbSet<lop> lops { get; set; }
        public DbSet<monhoc> monhocs { get; set; }
        public DbSet<nhom> nhoms { get; set; }
        public DbSet<pdkmh> pdkmhs { get; set; }
        public DbSet<phong> phongs { get; set; }
        public DbSet<sinhvien> sinhviens { get; set; }
        public DbSet<Thi> This { get; set; }
        public DbSet<sysdiagram> sysdiagrams { get; set; }
    }
}
