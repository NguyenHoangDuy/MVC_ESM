using Mvc_ESM.Static_Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mvc_ESM.Controllers
{
    public class FixSubjectController : Controller
    {
        //
        // GET: /FixSubject/

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult LoadGroupsBySubjectID(string SubjectID)
        {
            var Groups = (from m in InputHelper.db.This
                          where m.MaMonHoc == SubjectID
                          select new
                          {
                              Nhom = m.Nhom,
                              NgayThi = m.CaThi.GioThi
                          }).Distinct().ToList();

            if (Groups.Count() > 0)
            {
                var Subject = InputHelper.Groups[SubjectID + "_" + Groups[0].Nhom.Split(',')[0]];
                return Json(new
                {
                    MSMH = SubjectID,
                    TenMH = Subject.TenMonHoc,
                    TenKhoa = Subject.TenKhoa,
                    TenBM = Subject.TenBoMon,
                    Groups = Groups
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { MSMH = "false" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult CountStudent(string SubjectID, string Class)
        {
            IEnumerable<String> Result = InputHelper.db.Database.SqlQuery<String>("select sinhvien.MaSinhVien from pdkmh, sinhvien "
                                                                           + "where pdkmh.MaSinhVien = sinhvien.MaSinhVien "
                                                                           + "and MaMonHoc = '" + SubjectID + "' "
                                                                           + "and pdkmh.Nhom in(" + Class + ") "
                                                                           + "order by (Ten + Ho)");
            return Json(new
            {
                NumberStudent = Result.ToList<String>().Count.ToString()
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult CheckSubject(String SubjectID, String Class, long DateMilisecond, int Shift)
        {
            DateTime realDate = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddMilliseconds(DateMilisecond).Date + InputHelper.Options.Times[Shift].TimeOfDay;
            var MaCa = InputHelper.db.Database.SqlQuery<String>("select MaCa  from CaThi " +
                                                                "where GioThi='" + realDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
                                                                ).ToList();
            //lấy danh sách sinh viên môn xếp bằng tay
            var StudentList = InputHelper.db.Database.SqlQuery<String>("select pdkmh.MaSinhVien from pdkmh, sinhvien " +
                                                                "where pdkmh.MaSinhVien = sinhvien.MaSinhVien and MaMonHoc = '" + SubjectID + "' and Nhom in (" + Class + ") "
                                                                ).ToList();
            foreach (var mc in MaCa)
            {
                var Student = InputHelper.db.This.Where(m => m.MaCa == mc);
                foreach (var st in Student)
                    if (StudentList.Contains(st.MaSinhVien))
                        return Json(new { Ok = "false" }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { Ok = "true" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SelectSuccess(String SubjectID, String Classes, string Time, List<String> Room, List<int> Num)
        {
            var Date = DateTime.ParseExact(Time, "dd/MM/yyyy HH:mm", new CultureInfo("en-US"));
            String[] aClass = Classes.Split(',');
            List<String> Class = new List<String>();
            for (int i = 0; i < aClass.Length; i++)
                Class.Add(aClass[i]);
            OutputHelper.SaveOBJ("FixSubject", new { SubjectID, Class, Date, Room, Num });
            Process.Start(OutputHelper.WinAppExe, "6");
            return Json(new { SubjectID, Class, Date, Room, Num }, JsonRequestBehavior.AllowGet);
        }
    }
}
