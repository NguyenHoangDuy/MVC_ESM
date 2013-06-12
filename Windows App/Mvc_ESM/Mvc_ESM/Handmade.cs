using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mvc_ESM.Static_Helper
{
    class Handmade
    {
        public class HandmadeData
        {
            public String SubjectID { get; set; }
            public List<String> Class { get; set; }
            public DateTime Date { get; set; }
            public List<String> Room { get; set; }
            public List<int> Num { get; set; }
        }
        class StudentInfo
        {
            public string MaSinhVien { get; set; }
            public byte Nhom { get; set; }
        }

        public static void Run()
        {
            AlgorithmRunner.IsBusy = true;
            AlgorithmRunner.SaveOBJ("Status", "inf Đang lưu dữ liệu xếp lịch thủ công");
            Save(AlgorithmRunner.HandmadeData);
            AlgorithmRunner.SaveOBJ("Status", "inf Hoàn tất lưu dữ liệu xếp lịch thủ công");
            AlgorithmRunner.IsBusy = false;
        }

        public static void RunFixSubJect()
        {
            AlgorithmRunner.IsBusy = true;
            AlgorithmRunner.SaveOBJ("Status", "inf Đang Xoá CSDL cũ");
            Delete(AlgorithmRunner.HandmadeData);
            AlgorithmRunner.SaveOBJ("Status", "inf Đang lưu dữ liệu xếp lịch");
            Save(AlgorithmRunner.HandmadeData);
            AlgorithmRunner.SaveOBJ("Status", "inf Hoàn tất lưu dữ liệu xếp lịch");
            AlgorithmRunner.IsBusy = false;
        }

        public static void Delete(HandmadeData Data)
        {
            String ClassList = "";
            foreach (String cl in Data.Class)
                ClassList += cl + ",";

            ClassList = ClassList.Remove(ClassList.Length - 1, 1);


            var SubjectID = Data.SubjectID;
            DKMHEntities db = new DKMHEntities();
            try
            {
                var MaCaQry = (from thi in InputHelper.db.This
                               where thi.MaMonHoc == SubjectID && thi.Nhom == ClassList
                               select new
                               {
                                   MaCa = thi.MaCa,
                                   MSMH = thi.MaMonHoc,
                                   Nhom = thi.Nhom
                               }).FirstOrDefault();

                db.Database.ExecuteSqlCommand("DELETE FROM Thi WHERE MaCa='" + MaCaQry.MaCa + "' and MaMonHoc='" + MaCaQry.MSMH + "' and Nhom='" + MaCaQry.Nhom + "'");

                var MC = InputHelper.db.This.Where(m => m.MaCa == MaCaQry.MaCa).FirstOrDefault();
                if (MC == null)
                    db.Database.ExecuteSqlCommand("DELETE FROM CaThi WHERE MaCa='" + MaCaQry.MaCa + "'");

                var DbName = Regex.Match(db.Database.Connection.ConnectionString, "initial\\scatalog=([^;]+)").Groups[1].Value;
                db.Database.ExecuteSqlCommand("DBCC SHRINKFILE (" + DbName + ", 1) ");
                db.Database.ExecuteSqlCommand("DBCC SHRINKFILE (" + DbName + "_log, 1) ");
            }
            catch
            {
                AlgorithmRunner.SaveOBJ("Status", "err Lỗi Pri trong khi xoá CSDL, hãy thử chạy lại lần nữa!");
                AlgorithmRunner.IsBusy = false;
                Thread.CurrentThread.Abort();
            }
        }

        public static void Save(HandmadeData Data)
        {
            DKMHEntities db = new DKMHEntities();
            var ClassList = "";
            foreach (String cl in Data.Class)
            {
                ClassList += (ClassList.Length > 0 ? ", " : "") + "'" + cl + "'";
            }

            var StudentList = db.Database.SqlQuery<StudentInfo>("select pdkmh.MaSinhVien, pdkmh.Nhom from pdkmh, sinhvien " +
                                                                "where pdkmh.MaSinhVien = sinhvien.MaSinhVien and MaMonHoc = '" + Data.SubjectID + "' and Nhom in (" + ClassList + ") "
                                                                ).ToList();

            var DotQry = (from m in InputHelper.db.This
                          select m.Dot).Max();
            int dot = 1;
            if (DotQry != null)
            {
                dot = int.Parse(DotQry[0].ToString());
            }

            DateTime FirstShiftTime = InputHelper.Options.StartDate.AddHours(InputHelper.Options.Times[0].Hour)
                                                                      .AddMinutes(InputHelper.Options.Times[0].Minute);
            String ShiftID = dot.ToString();//InputHelper.Options.StartDate.Year + "" + InputHelper.Options.StartDate.Month + "" + InputHelper.Options.StartDate.Day;
            ShiftID += "_" + RoomArrangement.CalcShift(FirstShiftTime, Data.Date).ToString();
            if ((from ct in db.CaThis where ct.MaCa == ShiftID select ct).Count() == 0)
            {
                var pa = new SqlParameter[] 
                        { 
                            new SqlParameter("@MaCa", SqlDbType.NVarChar) { Value = ShiftID },
                            new SqlParameter("@GioThi", SqlDbType.DateTime) { Value = Data.Date },
                        };
                db.Database.ExecuteSqlCommand("INSERT INTO CaThi (MaCa, GioThi) VALUES (@MaCa, @GioThi)", pa);
            }


            Thi aRecord = new Thi();
            aRecord.MaMonHoc = Data.SubjectID;
            aRecord.MaCa = ShiftID;
            ClassList = "";
            foreach (String cl in Data.Class)
                ClassList += cl + ",";
            ClassList = ClassList.Remove(ClassList.Length - 1, 1);
            aRecord.Nhom = ClassList;
            String SQLQuery = "";
            int StudentIndex = 0;
            for (int Index = 0; Index < Data.Room.Count; Index++)
            {
                aRecord.MaPhong = Data.Room[Index];
                for (int i = 0; i < Data.Num[Index]; i++)
                {
                    aRecord.MaSinhVien = StudentList[StudentIndex].MaSinhVien;
                    SQLQuery += String.Format("INSERT INTO Thi (MaCa, MaMonHoc, Nhom, MaPhong, MaSinhVien, Dot) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}','{5}')\r\n",
                                                aRecord.MaCa,
                                                aRecord.MaMonHoc,
                                                aRecord.Nhom,
                                                aRecord.MaPhong,
                                                aRecord.MaSinhVien,
                                                dot
                                            );
                    StudentIndex++;
                }
            }
            db.Database.ExecuteSqlCommand(SQLQuery);
        }
    }
}
