using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mvc_ESM.Static_Helper;
using System.Threading;

namespace Mvc_ESM
{
    class RoomPriority
    {
        public static List<String> Groups;
        private static Dictionary<String, List<String>> StudentByGroup;
        public static List<Room>[] GroupsRoom;
        public static List<String>[][] GroupsRoomStudents;
        public static DateTime[] GroupsTime;
        private static int RoomUsedIndex;
        private static int MaxContaint;
        public static int Count;


        public static void Run()
        {
            InitGroup();
            GetStudentList();
            Arrangement();
            AlgorithmRunner.SaveOBJ("GroupsTimePri", RoomPriority.GroupsTime);
            AlgorithmRunner.SaveOBJ("GroupsRoomPri", RoomPriority.GroupsRoom);
            AlgorithmRunner.SaveOBJ("GroupsRoomStudentsPri", RoomPriority.GroupsRoomStudents);
            UpdateShiftsAndRooms();
            AlgorithmRunner.SaveOBJ("AppShifts", InputHelper.Shifts);
            AlgorithmRunner.SaveOBJ("AppRooms", InputHelper.Rooms);
        }

        public static void InitGroup()
        {
            Groups = new List<string>();
            GroupsTime = new DateTime[InputHelper.Priorities.Count];
            int i = 0;
            foreach (var pri in InputHelper.Priorities)
            {
                String st = pri.SubjectID + "_" + GetClass(pri.SubjectID);
                Groups.Add(st);
                GroupsTime[i++] = pri.Time;
            }
            Count = InputHelper.Priorities.Count;
            MaxContaint = InputHelper.Rooms.Max(m => m.Rooms.Where(w => !w.IsBusy).Sum(s => s.Container));
        }

        public static String GetClass(String SubjectID)
        {
            String st = "";
            var nhom = (from m in InputHelper.db.nhoms
                        where m.MaMonHoc.Equals(SubjectID)
                        select m.Nhom1).ToList();

            foreach (var n in nhom)
                st += n + "_";
            st = st.Remove(st.Length - 1, 1);
            return st;
        }

        private static void GetStudentList()
        {
            StudentByGroup = new Dictionary<String, List<String>>();
            for (int GroupIndex = 0; GroupIndex < Groups.Count; GroupIndex++)
            {
                String SubjectID = AlgorithmRunner.GetSubjectID(Groups[GroupIndex]);
                String ClassList = AlgorithmRunner.GetClassList(Groups[GroupIndex]);
                IEnumerable<String> Result = InputHelper.db.Database.SqlQuery<String>("select sinhvien.MaSinhVien from pdkmh, sinhvien "
                                                                            + "where pdkmh.MaSinhVien = sinhvien.MaSinhVien "
                                                                            + "and MaMonHoc = '" + SubjectID + "' "
                                                                            + "and pdkmh.Nhom in(" + ClassList + ") "
                                                                            + "order by (Ten + Ho)");
                StudentByGroup.Add(Groups[GroupIndex], Result.ToList<String>());
            }
            GroupsRoom = new List<Room>[Groups.Count];
            GroupsRoomStudents = new List<String>[Groups.Count][];
        }


        private static void Arrangement()
        {
            for (int GroupIndex = 0; GroupIndex < InputHelper.Priorities.Count; GroupIndex++)
            {
                var RoomList = InputHelper.Rooms.Find(m => m.Time.Date == GroupsTime[GroupIndex].Date).Rooms.Where(r => !r.IsBusy).ToList();
                RoomArrangementForOneGroup(GroupIndex, RoomList);
            }
        }

        private static void RoomArrangementForOneGroup(int GroupIndex, List<Room> RoomList)
        {
            int StudentsNumber = StudentByGroup[Groups[GroupIndex]].Count;
            if (StudentsNumber > MaxContaint)
            {
                AlgorithmRunner.SaveOBJ("Status", "err Phòng thi không đủ, đang dừng lại ở nhóm thi: " + AlgorithmRunner.Groups[GroupIndex] + " với số sinh viên là:" + StudentsNumber);
                AlgorithmRunner.IsBusy = false;
                Thread.CurrentThread.Abort();
            }

            GroupsRoom[GroupIndex] = new List<Room>();
            int OldRoomUsedIndex = RoomUsedIndex;
            while (StudentsNumber > 0)
            {
                RoomUsedIndex++;
                if (RoomUsedIndex < RoomList.Count) // còn phòng
                {

                    GroupsRoom[GroupIndex].Add(RoomList[RoomUsedIndex]);
                    StudentsNumber -= RoomList[RoomUsedIndex].Container;
                    // đáng lẽ code phân trực tiếp sv vào phòng ở đây, nhưng như vậy phòng ít phòng nhiều
                    // để đó sau này truy vấn lại môn này thi mấy phòng rồi chia sau!
                }
            }
            MakeStudentList(GroupIndex);
        }

        private static void MakeStudentList(int GroupIndex)
        {
            //tổng số sinh viên
            int StudentsNumber = StudentByGroup[Groups[GroupIndex]].Count;
            //số phòng cần sử dụng
            int RoomNumber = GroupsRoom[GroupIndex].Count;
            //số sv trung bình cho 1 phòng
            int StudentsPerRoom = StudentsNumber / RoomNumber;
            GroupsRoomStudents[GroupIndex] = new List<String>[RoomNumber];
            int Used = 0;
            //số dư
            int OverLoad = StudentsNumber - StudentsPerRoom * RoomNumber;
            for (int RoomIndex = 0; RoomIndex < RoomNumber; RoomIndex++)
            {
                int Use;
                //if số sv 1 phòng + số dư > sức chứa
                if (StudentsPerRoom + OverLoad > GroupsRoom[GroupIndex][RoomIndex].Container)
                {
                    Use = GroupsRoom[GroupIndex][RoomIndex].Container; //full
                    OverLoad = (StudentsPerRoom + OverLoad) - Use;
                }
                else
                {
                    Use = StudentsPerRoom + OverLoad;
                    OverLoad = 0;
                }
                GroupsRoomStudents[GroupIndex][RoomIndex] = new List<String>();
                for (int StudentIndex = Used; StudentIndex < Used + Use; StudentIndex++)
                {
                    GroupsRoomStudents[GroupIndex][RoomIndex].Add(StudentByGroup[Groups[GroupIndex]][StudentIndex]);

                }
                Used += Use;
            }
        }

        private static void UpdateShiftsAndRooms()
        {
            for (int Index = 0; Index < GroupsTime.Length; Index++)
            {
                int ShiftIndex = InputHelper.Shifts.FindIndex(m => m.Time == GroupsTime[Index]);
                InputHelper.Shifts[ShiftIndex].IsBusy = true;
                int RoomListIndex = InputHelper.Rooms.FindIndex(m => m.Time == GroupsTime[Index]);
                foreach (Room aRoom in GroupsRoom[Index])
                {
                    int RoomIndex = InputHelper.Rooms[RoomListIndex].Rooms.FindIndex(m => m.RoomID == aRoom.RoomID);
                    InputHelper.Rooms[RoomListIndex].Rooms[RoomIndex].IsBusy = true;
                }
            }
        }

    }
}
