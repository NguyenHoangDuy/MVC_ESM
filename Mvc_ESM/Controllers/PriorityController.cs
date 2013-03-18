using Model;
using Mvc_ESM.Static_Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace Mvc_ESM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PriorityController : Controller
    {
        //
        // GET: /Priority/
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public String SelectSuccess(List<String> SubjectID, List<long> Date, List<long> Time)
        {
            string paramInfo = "";
            InputHelper.SubjectPriority = new List<Priority>();
            List<String> SB = new List<String>();
            List<String> aClass = new List<String>();
            List<String> Check = new List<String>();
            for (int i = 0; i < SubjectID.Count(); i++)
            {
                DateTime dt = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddMilliseconds(Date[i]).Date;
                InputHelper.SubjectPriority.Add(new Priority
                {
                    SubjectID = SubjectID[i],
                    Date = dt,
                    Time = dt.AddMilliseconds(Time[i])
                });
                var a = SubjectID[i];
                var nhom = (from m in InputHelper.db.nhoms
                            where m.MaMonHoc.Equals(a)
                            select m.Nhom1).ToList();

                foreach (var r in nhom)
                {
                    SB.Add(SubjectID[i]);
                    aClass.Add(r.ToString());
                    Check.Add("checked");
                }
                paramInfo += "MH:" + SubjectID[i] + " Ngay:" + Date[i] + "Gio:" + Time[i] + "<br /><br />";
            }
            OutputHelper.SaveOBJ("SubjectPriority", InputHelper.SubjectPriority);
            string st = OutputHelper.SaveIgnoreGroups(SB, aClass, Check, true);
            return paramInfo;
        }

    }
}
