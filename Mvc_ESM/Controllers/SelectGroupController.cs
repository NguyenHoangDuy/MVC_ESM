using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Model;
using System.Collections.Specialized;
using System.Collections;
using Mvc_ESM.Static_Helper;
using System.Text;

namespace Mvc_ESM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SelectGroupController : Controller
    {
        [HttpGet]
        public ViewResult Index()
        {
            return View();
        }

        [HttpGet]
        public ViewResult IgnoreList()
        {
            return View();
        }

        [HttpPost]
        public String IgnoreSuccess(List<String> SubjectID, List<String> Class, List<String> Check)
        {
            List<Priority> SBP = InputHelper.SubjectPriority;

            InputHelper.SubjectPriority = new List<Priority>();

            foreach (var sbp in SBP)
                if (!SubjectID.Contains(sbp.SubjectID))
                {
                    InputHelper.SubjectPriority.Add(new Priority
                    {
                        SubjectID = sbp.SubjectID,
                        Date = sbp.Date,
                        Time = sbp.Time
                    });
                }

            OutputHelper.SaveOBJ("SubjectPriority", InputHelper.SubjectPriority);

            return OutputHelper.SaveIgnoreGroups(SubjectID, Class, Check, true);
        }

        [HttpPost]
        public String SelectSuccess(List<String> SubjectID, List<String> Class, List<int> Group)
        {
            return OutputHelper.SaveGroups(SubjectID, Class, Group, true);
        }

        [HttpPost]
        public String IgnoreCancel()
        {
            CurrentSession.Reset("IgnoreGroups");
            //CurrentSession.Set("IgnoreGroups", Clone.Dictionary<String, Group>(InputHelper.Groups));
            return "Cancel";
        }

        [HttpPost]
        public String SelectCancel()
        {
            CurrentSession.Reset("Groups");
            //CurrentSession.Set("Groups", Clone.Dictionary<String, Group>(InputHelper.Groups));
            return "Cancel";
        }
    }
}