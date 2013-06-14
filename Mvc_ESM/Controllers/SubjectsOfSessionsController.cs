using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Model;
using Mvc_ESM.Static_Helper;
using System.Text.RegularExpressions;

namespace Mvc_ESM.Controllers
{
    public class SubjectsOfSessionsController : Controller
    {
        private DKMHEntities db = new DKMHEntities();

        [HttpGet]
        public ViewResult Index()
        {
            InitViewBag();
            List<string[]> Result = new List<string[]>();

            /*var Thi = (from t in InputHelper.db.This
                       join m in InputHelper.db.monhocs on t.MaMonHoc equals m.MaMonHoc
                       select new { t.MaMonHoc, m.TenMonHoc, t.Nhom, t.CaThi.GioThi }).Distinct();

            foreach (var t in Thi)
            {
                string[] s = new string[5];
                s[0] = t.MaMonHoc;
                s[1] = t.TenMonHoc;
                s[2] = t.Nhom;
                s[3] = t.GioThi.Date.ToShortDateString();
                s[4] = t.GioThi.ToString("HH:mm");
                Result.Add(s);
            }*/
            return View(Result);
        }


        [HttpPost]
        public ViewResult Index(string Dot)
        {
            InitViewBag();
            List<string[]> Result = new List<string[]>();

            var Thi = (from t in InputHelper.db.This
                       join m in InputHelper.db.monhocs on t.MaMonHoc equals m.MaMonHoc
                       where t.Dot == Dot || Dot == ""
                       select new { t.MaMonHoc, m.TenMonHoc, t.Nhom, t.CaThi.GioThi, t.MaCa }).Distinct();

            foreach (var t in Thi)
            {
                string[] s = new string[6];
                s[0] = t.MaMonHoc;
                s[1] = t.TenMonHoc;
                s[2] = t.Nhom;
                s[3] = t.GioThi.Date.ToShortDateString();
                s[4] = t.GioThi.ToString("HH:mm");
                s[5] = t.MaCa.ToString();
                Result.Add(s);
            }

            return View(Result);
        }


        private void InitViewBag()
        {
            var DotQry = (from m in InputHelper.db.This
                          select new
                          {
                              MaDot = m.Dot,
                              TenDon = m.Dot
                          }).Distinct().OrderBy(m => m.MaDot);
            ViewBag.Dot = new SelectList(DotQry.ToArray(), "MaDot", "TenDon");
        }

        [HttpPost]
        public ActionResult Linker(string id)
        {
            return RedirectToAction( "Index", "FixSubject", new { sid = id });
        }


        [HttpPost, ActionName("Delete")]
        public String Delete(string id)
        {
            try
            {
                String[] str = id.Split('_');
                String MSMH = str[0];
                String MaNhom = str[1];
                var aThi = InputHelper.db.This.Where(m => m.MaMonHoc.Equals(MSMH)).Where(m => m.Nhom.Equals(MaNhom)).First();
                string maca = aThi.MaCa.ToString();

                var room = (from r in InputHelper.db.This
                            where r.MaMonHoc == MSMH && r.Nhom == MaNhom
                            select new
                            {
                                r.MaPhong,
                                r.CaThi.GioThi
                            }).Distinct();


                foreach (var r in room)
                {
                    int index = InputHelper.BusyRooms.Find(m => m.Time == r.GioThi).Rooms.FindIndex(m => m.RoomID == r.MaPhong);
                    InputHelper.BusyRooms.Find(m => m.Time == r.GioThi).Rooms[index].IsBusy = false;
                }
                OutputHelper.SaveOBJ("Rooms", InputHelper.BusyRooms);


                InputHelper.db.DelThi(MSMH, MaNhom);

                var Count = InputHelper.db.This.Where(m => m.MaCa.Equals(maca)).Count();
                
               
                if (Count == 0)
                {
                    db.DelCaThi(maca);
                }
                return "Xoá thành công!";
            }
            catch (Exception e)
            {
                return "Xoá không được [" + e.Message + "]";
            }
        }

    }
}