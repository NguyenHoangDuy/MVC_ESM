using Model;
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
    class SaveToDatabase
    {
        private static DKMHEntities db = new DKMHEntities();
        public static void Run()
        {
            AlgorithmRunner.IsBusy = true;
            //AlgorithmRunner.SaveOBJ("Status", "inf Đang Xoá CSDL cũ");
            //DeleteOld();
            AlgorithmRunner.SaveOBJ("Status", "inf Đang Lưu vào cơ sở dữ liệu");
            Save();
            AlgorithmRunner.SaveOBJ("Status", "inf Hoàn tất quá trình lưu!");
            AlgorithmRunner.IsBusy = false;
            //        Thread.CurrentThread.Abort();
        }

        public static void Delete(object Dot)
        {
            AlgorithmRunner.IsBusy = true;
            AlgorithmRunner.SaveOBJ("Status", "inf Đang Xoá CSDL cũ");

            if (Dot.ToString() == "")
                DeleteOld();
            else
                DeleteOld((string)Dot);
            AlgorithmRunner.SaveOBJ("Status", "inf Hoàn tất quá trình Xoá CSDL!");
            AlgorithmRunner.IsBusy = false;
        }

        private static void DeleteOld()
        {
            try
            {
                db.Database.ExecuteSqlCommand("DELETE FROM Thi");
                db.Database.ExecuteSqlCommand("DELETE FROM CaThi");
                var DbName = Regex.Match(db.Database.Connection.ConnectionString, "initial\\scatalog=([^;]+)").Groups[1].Value;
                db.Database.ExecuteSqlCommand("DBCC SHRINKFILE (" + DbName + ", 1) ");
                db.Database.ExecuteSqlCommand("DBCC SHRINKFILE (" + DbName + "_log, 1) ");
            }
            catch
            {
                AlgorithmRunner.SaveOBJ("Status", "err Lỗi trong khi xoá CSDL, hãy thử chạy lại lần nữa!");
                AlgorithmRunner.IsBusy = false;
                Thread.CurrentThread.Abort();
            }
        }

        private static void DeleteOld(string dot)
        {
            try
            {
                var MaCaQry = (from ca in InputHelper.db.This
                               where ca.Dot == dot
                               select new { MaCa = ca.MaCa }).Distinct().ToList();

                db.Database.ExecuteSqlCommand("DELETE FROM Thi WHERE Dot='" + dot + "'");
                foreach (var ca in MaCaQry)
                    db.Database.ExecuteSqlCommand("DELETE FROM CaThi WHERE MaCa='" + ca.MaCa + "'");

                var DbName = Regex.Match(db.Database.Connection.ConnectionString, "initial\\scatalog=([^;]+)").Groups[1].Value;
                db.Database.ExecuteSqlCommand("DBCC SHRINKFILE (" + DbName + ", 1) ");
                db.Database.ExecuteSqlCommand("DBCC SHRINKFILE (" + DbName + "_log, 1) ");
            }
            catch
            {
                AlgorithmRunner.SaveOBJ("Status", "err Lỗi trong khi xoá CSDL, hãy thử chạy lại lần nữa!");
                AlgorithmRunner.IsBusy = false;
                Thread.CurrentThread.Abort();
            }
        }
        private static void Save()
        {

            var DotQry = (from m in InputHelper.db.This
                          select m.Dot).Max();
            int dot = 1;
            if (DotQry != null)
            {
                dot = int.Parse(DotQry[0].ToString()) + 1;
            }

            int GCount = RoomPriority.Groups.Count;
            for (int GroupIndex = 0; GroupIndex < GCount; GroupIndex++)
            {
                Thi aRecord = new Thi();
                aRecord.MaMonHoc = AlgorithmRunner.GetSubjectID(RoomPriority.Groups[GroupIndex]);
                aRecord.Nhom = AlgorithmRunner.GetClassList(RoomPriority.Groups[GroupIndex]);


                DateTime FirstShiftTime = InputHelper.Options.StartDate.AddHours(InputHelper.Options.Times[0].Hour)
                                                                      .AddMinutes(InputHelper.Options.Times[0].Minute);
                String ShiftID = dot.ToString();// InputHelper.Options.StartDate.Year + "" + InputHelper.Options.StartDate.Month + "" + InputHelper.Options.StartDate.Day;
                ShiftID += RoomArrangement.CalcShift(FirstShiftTime, RoomPriority.GroupsTime[GroupIndex]).ToString() + "UT";


                if ((from ct in db.CaThis where ct.MaCa == ShiftID select ct).Count() == 0)
                {

                    var pa = new SqlParameter[] 
                        { 
                            new SqlParameter("@MaCa", SqlDbType.NVarChar) { Value = ShiftID},
                            new SqlParameter("@GioThi", SqlDbType.DateTime) { Value = RoomPriority.GroupsTime[GroupIndex] },
                        };
                    db.Database.ExecuteSqlCommand("INSERT INTO CaThi (MaCa, GioThi) VALUES (@MaCa, @GioThi)", pa);
                }
                aRecord.MaCa = ShiftID;
                String SQLQuery = "";
                for (int RoomIndex = 0; RoomIndex < RoomPriority.GroupsRoom[GroupIndex].Count; RoomIndex++)
                {
                    aRecord.MaPhong = RoomPriority.GroupsRoom[GroupIndex][RoomIndex].RoomID;
                    for (int StudentIndex = 0; StudentIndex < RoomPriority.GroupsRoomStudents[GroupIndex][RoomIndex].Count; StudentIndex++)
                    {
                        aRecord.MaSinhVien = RoomPriority.GroupsRoomStudents[GroupIndex][RoomIndex][StudentIndex];
                        SQLQuery += String.Format("INSERT INTO Thi (MaCa, MaMonHoc, Nhom, MaPhong, MaSinhVien, Dot) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}','{5}')\r\n",
                                                    aRecord.MaCa,
                                                    aRecord.MaMonHoc,
                                                    aRecord.Nhom,
                                                    aRecord.MaPhong,
                                                    aRecord.MaSinhVien,
                                                    dot
                                                );
                    } // sinh viên
                } // phòng
                try
                {
                    AlgorithmRunner.SaveOBJ("Status", "inf Đang Lưu vào cơ sở dữ liệu (" + (GroupIndex + 1) + "/" + GCount + ")");
                    db.Database.ExecuteSqlCommand(SQLQuery);
                }
                catch
                {
                    AlgorithmRunner.SaveOBJ("Status", "err Lỗi trong khi chèn thêm nội dung vào CSDL! Hãy thử lại hoặc liên hệ với quản trị nếu vẫn lỗi!");
                    AlgorithmRunner.IsBusy = false;
                    Thread.CurrentThread.Abort();
                }
            }// môn


            GCount = AlgorithmRunner.Groups.Count;
            for (int GroupIndex = 0; GroupIndex < GCount; GroupIndex++)
            {
                Thi aRecord = new Thi();
                aRecord.MaMonHoc = AlgorithmRunner.GetSubjectID(AlgorithmRunner.Groups[GroupIndex]);
                aRecord.Nhom = AlgorithmRunner.GetClassList(AlgorithmRunner.Groups[GroupIndex]);


                DateTime FirstShiftTime = InputHelper.Options.StartDate.AddHours(InputHelper.Options.Times[0].Hour)
                                                                      .AddMinutes(InputHelper.Options.Times[0].Minute);
                String ShiftID = dot.ToString();// InputHelper.Options.StartDate.Year + "" + InputHelper.Options.StartDate.Month + "" + InputHelper.Options.StartDate.Day;
                ShiftID += RoomArrangement.CalcShift(FirstShiftTime, AlgorithmRunner.GroupsTime[GroupIndex]).ToString();


                if ((from ct in db.CaThis where ct.MaCa == ShiftID select ct).Count() == 0)
                {

                    var pa = new SqlParameter[] 
                        { 
                            new SqlParameter("@MaCa", SqlDbType.NVarChar) { Value = ShiftID},
                            new SqlParameter("@GioThi", SqlDbType.DateTime) { Value = AlgorithmRunner.GroupsTime[GroupIndex] },
                        };
                    db.Database.ExecuteSqlCommand("INSERT INTO CaThi (MaCa, GioThi) VALUES (@MaCa, @GioThi)", pa);
                }
                aRecord.MaCa = ShiftID;
                String SQLQuery = "";
                for (int RoomIndex = 0; RoomIndex < AlgorithmRunner.GroupsRoom[GroupIndex].Count; RoomIndex++)
                {
                    aRecord.MaPhong = AlgorithmRunner.GroupsRoom[GroupIndex][RoomIndex].RoomID;
                    for (int StudentIndex = 0; StudentIndex < AlgorithmRunner.GroupsRoomStudents[GroupIndex][RoomIndex].Count; StudentIndex++)
                    {
                        aRecord.MaSinhVien = AlgorithmRunner.GroupsRoomStudents[GroupIndex][RoomIndex][StudentIndex];
                        SQLQuery += String.Format("INSERT INTO Thi (MaCa, MaMonHoc, Nhom, MaPhong, MaSinhVien, Dot) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}','{5}')\r\n",
                                                    aRecord.MaCa,
                                                    aRecord.MaMonHoc,
                                                    aRecord.Nhom,
                                                    aRecord.MaPhong,
                                                    aRecord.MaSinhVien,
                                                    dot
                                                );
                    } // sinh viên
                } // phòng
                try
                {
                    AlgorithmRunner.SaveOBJ("Status", "inf Đang Lưu vào cơ sở dữ liệu (" + (GroupIndex + 1) + "/" + GCount + ")");
                    db.Database.ExecuteSqlCommand(SQLQuery);
                }
                catch
                {
                    AlgorithmRunner.SaveOBJ("Status", "err Lỗi trong khi chèn thêm nội dung vào CSDL! Hãy thử lại hoặc liên hệ với quản trị nếu vẫn lỗi!");
                    AlgorithmRunner.IsBusy = false;
                    Thread.CurrentThread.Abort();
                }
            }// môn
        }

    }
}
