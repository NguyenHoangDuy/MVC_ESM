using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Model;
using Mvc_ESM.Static_Helper;

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

            return View(Result);
        }


        [HttpPost]
        public ViewResult Index(string Dot)
        {
            InitViewBag();
            List<string[]> Result = new List<string[]>();

            var Thi = (from t in InputHelper.db.This
                       join m in InputHelper.db.monhocs on t.MaMonHoc equals m.MaMonHoc
                       where t.Dot == Dot || Dot==""
                       select new { t.MaMonHoc, m.TenMonHoc, t.Nhom, t.CaThi.GioThi }).Distinct();

            foreach (var t in Thi)
            {
                string[] s = new string[5];
                s[0] = t.MaMonHoc;
                s[1] = t.TenMonHoc;
                s[2] = t.TenMonHoc;
                s[3] = t.GioThi.Date.ToShortDateString();
                s[4] = t.GioThi.ToString("HH:mm");
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
                          }).Distinct();
            ViewBag.Dot = new SelectList(DotQry.ToArray(), "MaDot", "TenDon");
        }


    }
}