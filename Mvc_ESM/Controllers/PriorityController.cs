using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Mvc_ESM.Static_Helper;
using System.Text;

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
        public String SelectSuccess(List<long> Date, List<String> SubjectID, List<long> Time)
        {
            string paramInfo = "";
            InputHelper.SubjectPriority = new List<Priority>();
            for (int i = 0; i < SubjectID.Count(); i++)
            {

                InputHelper.SubjectPriority.Add(new Priority
                {
                    SubjectID = SubjectID[i],
                    Date = (new DateTime(1970, 1, 1, 0, 0, 0, 0)).AddMilliseconds(Date[i]).Date,
                    Time = InputHelper.SubjectPriority[i].Date.AddMilliseconds(Time[i])
                });
                paramInfo += "MH:" + SubjectID[i] + " Ngay:" + Date[i] + "Gio:" + Time[i] + "<br /><br />";
            }
            OutputHelper.SaveOBJ("SubjectPriority", InputHelper.SubjectPriority);
            return paramInfo;
        }

    }
}
