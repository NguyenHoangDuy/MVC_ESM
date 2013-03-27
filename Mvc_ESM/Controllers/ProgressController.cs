using Mvc_ESM.Static_Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Model;

namespace Mvc_ESM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProgressController : Controller
    {
        //
        // GET: /Progress/
        public ActionResult Index()
        {
            OutputHelper.SaveOBJ("Status", "");
            var DotQry = (from m in InputHelper.db.This
                          select new
                          {
                              MaDot = m.Dot,
                              TenDon = m.Dot
                          }).Distinct();
            ViewBag.Dot = new SelectList(DotQry.ToArray(), "MaDot", "TenDon");
            return View();
        }

        private void UnCheckSubject()
        {
            var aGroupList = InputHelper.Groups.Where(d => !d.Value.IsIgnored);
            var SubjectsList = aGroupList.Select(m => m.Key.Substring(0, m.Key.IndexOf('_'))).Distinct();
            List<String> SB = new List<string>();
            List<String> aClass = new List<string>();
            List<String> Check = new List<string>();
            foreach (var subject in SubjectsList)
            {
                var GroupsInOneSubject = aGroupList.Where(m => m.Value.MaMonHoc == subject).Select(m => m.Value);
                var GroupsIDList = GroupsInOneSubject.Select(m => m.GroupID).Distinct();
                foreach (var aID in GroupsIDList)
                {
                    //var aGroupItem = subject;
                    foreach (var gi in GroupsInOneSubject.Where(m => m.GroupID == aID))
                    {
                        SB.Add(subject);
                        //  aGroupItem += "_" + gi.Nhom;
                        aClass.Add(gi.Nhom.ToString());
                        Check.Add("checked");
                    }
                }
            }
            string st = OutputHelper.SaveIgnoreGroups(SB, aClass, Check, true);
        }

        [HttpPost]
        public ActionResult Run(string StepNumber)
        {
            switch (int.Parse(StepNumber.Substring(0, 1)))
            {
                case 0:
                    Process.Start(OutputHelper.WinAppExe, "0");
                    return Content("RunStop");
                case 1:
                    Process.Start(OutputHelper.WinAppExe, "1");
                    return Content("RunCreateAdjacencyMatrix");
                case 2:
                    Process.Start(OutputHelper.WinAppExe, "2");
                    return Content("RunCalc");
                case 3:
                    Process.Start(OutputHelper.WinAppExe, "3");
                    UnCheckSubject();
                    return Content("RunSaveToDatabase");
                case 5:
                    Process.Start(OutputHelper.WinAppExe, StepNumber);
                    return Content("RunSaveToDatabase");
                default:
                    return Content("NotRunAnyThing");
            }
        }
    }
}

