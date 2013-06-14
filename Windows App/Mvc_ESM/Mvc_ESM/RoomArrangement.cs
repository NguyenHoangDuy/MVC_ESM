using Model;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Mvc_ESM.Static_Helper
{
    class RoomArrangement
    {

        //- B1a: Lấy danh sách sinh viên sẽ thi mỗi môn, xếp theo ABC, lấy luôn số lượng
        //- B1b: Xếp danh sách môn trong 1 ca theo thứ tự sao cho khi xếp phòng số phòng được sử dụng là tối đa
        private static List<Shift> ShiftList;
        private static Dictionary<String, List<String>> StudentByGroup;
        /// <summary>
        /// Kiểm tra từng môn xem xử lý chưa
        /// </summary>
        private static Boolean[] Progressed;
        private static int RoomUsedIndex;
        private static int MaxContaint;
        private static int SumContaint;

        public static void Run()
        {
            Init();
            Arrangement();
            AlgorithmRunner.SaveOBJ("GroupsTime", AlgorithmRunner.GroupsTime);
            AlgorithmRunner.SaveOBJ("GroupsRoom", AlgorithmRunner.GroupsRoom);
            AlgorithmRunner.SaveOBJ("GroupsRoomStudents", AlgorithmRunner.GroupsRoomStudents);
        //    UpdateShiftsAndRooms();
            AlgorithmRunner.SaveOBJ("AppShifts", InputHelper.Shifts);
            AlgorithmRunner.SaveOBJ("AppRooms", InputHelper.Rooms);
        }

        
        /// <summary>
        /// tạo danh sách sinh viên cho môn học
        /// </summary>
        /// <param name="GroupIndex"></param>
        private static void MakeStudentList(int GroupIndex)
        {
            //tổng số sinh viên
            int StudentsNumber = StudentByGroup[AlgorithmRunner.Groups[GroupIndex]].Count;
            //số phòng cần sử dụng
            int RoomNumber = AlgorithmRunner.GroupsRoom[GroupIndex].Count;
            //số sv trung bình cho 1 phòng
            int StudentsPerRoom = StudentsNumber / RoomNumber;
            AlgorithmRunner.GroupsRoomStudents[GroupIndex] = new List<String>[RoomNumber];
            int Used = 0;
            //số dư
            int OverLoad = StudentsNumber - StudentsPerRoom * RoomNumber;
            for (int RoomIndex = 0; RoomIndex < RoomNumber; RoomIndex++)
            {
                int Use;
                //if số sv 1 phòng + số dư > sức chứa
                if (StudentsPerRoom + OverLoad > AlgorithmRunner.GroupsRoom[GroupIndex][RoomIndex].Container)
                {
                    Use = AlgorithmRunner.GroupsRoom[GroupIndex][RoomIndex].Container; //full
                    OverLoad = (StudentsPerRoom + OverLoad) - Use;
                }
                else
                {
                    Use = StudentsPerRoom + OverLoad;
                    OverLoad = 0;
                }
                AlgorithmRunner.GroupsRoomStudents[GroupIndex][RoomIndex] = new List<String>();
                for (int StudentIndex = Used; StudentIndex < Used + Use; StudentIndex++)
                {
                    AlgorithmRunner.GroupsRoomStudents[GroupIndex][RoomIndex].Add(RoomArrangement.StudentByGroup[AlgorithmRunner.Groups[GroupIndex]][StudentIndex]);

                }
                Used += Use;
            }
        }

        public static int CalcShift(DateTime OldTime, DateTime NewTime)
        {
            int Shift1Index = InputHelper.Shifts.FindIndex(m => m.Time == OldTime);
            int Shift2Index = InputHelper.Shifts.FindIndex(m => m.Time == NewTime);
            return Math.Abs(Shift1Index - Shift2Index);
        }

        private static DateTime IncTime(DateTime Time, int Shift)
        {
            int CurrentShiftIndex = ShiftList.FindIndex(m => m.Time == Time);
            if (CurrentShiftIndex + Shift > ShiftList.Count - 1)
            {
                AlgorithmRunner.SaveOBJ("Status", "err Số ca thi không đủ, đang dừng lại ở ca thi thứ: " + CurrentShiftIndex + Shift);
                AlgorithmRunner.IsBusy = false;
                Thread.CurrentThread.Abort();
            }
            return ShiftList[CurrentShiftIndex + Shift].Time;
        }

        /// <summary>
        /// các môn cùng màu sẽ cùng ca, cùng ngày thi
        /// </summary>
        private static void SetDefaultTime()
        {
            AlgorithmRunner.GroupsTime = new DateTime[AlgorithmRunner.Groups.Count];
            AlgorithmRunner.MaxColorTime = new DateTime[AlgorithmRunner.ColorNumber];
            // ca thi
            int ShiftIndex = 0;
            for (int ColorNumber = 1; ColorNumber < AlgorithmRunner.ColorNumber; ColorNumber++)
            {
                // các môn cùng màu sẽ cùng ca, cùng ngày thi
                for (int GroupIndex = 0; GroupIndex < AlgorithmRunner.Groups.Count; GroupIndex++)
                {
                    if (AlgorithmRunner.Colors[GroupIndex] == ColorNumber)
                    {
                        AlgorithmRunner.GroupsTime[GroupIndex] = ShiftList[ShiftIndex].Time;
                        AlgorithmRunner.MaxColorTime[ColorNumber] = AlgorithmRunner.GroupsTime[GroupIndex];
                    }
                }
                ShiftIndex += InputHelper.Options.DateMin + 1;
            }
        }
        /// <summary>
        /// Lấy danh sách sinh viên thi của từng môn học
        /// </summary>
        private static void GetStudentList()
        {
            int SumStudents = 0;
            StudentByGroup = new Dictionary<String, List<String>>();
            for (int GroupIndex = 0; GroupIndex < AlgorithmRunner.Groups.Count; GroupIndex++)
            {
                String SubjectID = AlgorithmRunner.GetSubjectID(AlgorithmRunner.Groups[GroupIndex]);
                String ClassList = AlgorithmRunner.GetClassList(AlgorithmRunner.Groups[GroupIndex]);
                //String IgnoreStudents = InputHelper.IgnoreStudents.ContainsKey(SubjectID) ? JsonConvert.SerializeObject(InputHelper.IgnoreStudents[SubjectID]) : "[]";
                //IgnoreStudents = IgnoreStudents.Substring(1, IgnoreStudents.Length - 2).Replace("\"", "'");
                IEnumerable<String> Result = InputHelper.db.Database.SqlQuery<String>("select sinhvien.MaSinhVien from pdkmh, sinhvien "
                                                                            + "where pdkmh.MaSinhVien = sinhvien.MaSinhVien "
                                                                            + "and MaMonHoc = '" + SubjectID + "' "
                    //                  + (IgnoreStudents.Length > 0 ? "and not(sinhvien.MaSinhVien in (" + IgnoreStudents + ")) " : "")
                                                                            + "and pdkmh.Nhom in(" + ClassList + ") "
                                                                            + "order by (Ten + Ho)");
                StudentByGroup.Add(AlgorithmRunner.Groups[GroupIndex], Result.ToList<String>());
                SumStudents += Result.ToList<String>().Count;
            }

            if (SumStudents > SumContaint)
            {
                AlgorithmRunner.SaveOBJ("Status", "err Phòng thi không đủ");
                AlgorithmRunner.IsBusy = false;
                Thread.CurrentThread.Abort();
            }

            AlgorithmRunner.GroupsRoom = new List<Room>[AlgorithmRunner.Groups.Count];
            AlgorithmRunner.GroupsRoomStudents = new List<String>[AlgorithmRunner.Groups.Count][];
        }


        private static void Init()
        {
            ShiftList = InputHelper.Shifts.Where(m => !m.IsBusy).ToList();
            Progressed = new Boolean[AlgorithmRunner.Groups.Count];
            MaxContaint = InputHelper.Rooms.Max(m => m.Rooms.Where(w => !w.IsBusy).Sum(s => s.Container));
            SumContaint = InputHelper.Rooms.Sum(m => m.Rooms.Where(w => !w.IsBusy).Sum(s => s.Container));
            SetDefaultTime();
            GetStudentList();

            //InputHelper.Rooms = InputHelper.Rooms.OrderBy(r => r.Container).ToList<Room>();
        }

        /// <summary>
        /// Chia phòng cho từng môn cùng ca
        /// </summary>
        private static void Arrangement()
        {
            List<int> GroupIndexList;
            //lấy danh sách các nhóm chưa xử lý
            GroupIndexList = GetNextShiftGroups();//.ToList();
            //int x = SubjectsIndexList.Count();
            while (GroupIndexList.Count() > 0)
            {
                var RoomList = InputHelper.Rooms.Find(m => m.Time.Date == AlgorithmRunner.GroupsTime[GroupIndexList[0]].Date).Rooms.Where(r => !r.IsBusy).ToList();
                RoomUsedIndex = -1; // gán bằng -1 để vào xếp phòng nó tăng lên xét coi còn phòng ko
                foreach (int si in GroupIndexList)
                {
                    RoomArrangementForOneGroup(si, RoomList);
                }
                GroupIndexList = GetNextShiftGroups();
            }
        }
        /// <summary>
        /// B3: Tăng thời gian của tất cả các môn có màu khác và thi sau lên 1 khoảng sao cho > max[màu các môn vừa tăng thời gian]
        /// </summary>
        /// <param name="GroupIndex"></param>
        /// <param name="RoomList"></param>
        private static void RoomArrangementForOneGroup(int GroupIndex, List<Room> RoomList)
        {
            int StudentsNumber = StudentByGroup[AlgorithmRunner.Groups[GroupIndex]].Count;
            if (StudentsNumber > MaxContaint)
            {
                AlgorithmRunner.SaveOBJ("Status", "err Phòng thi không đủ, đang dừng lại ở nhóm thi: " + AlgorithmRunner.Groups[GroupIndex] + " với số sinh viên là:" + StudentsNumber);
                AlgorithmRunner.IsBusy = false;
                Thread.CurrentThread.Abort();
            }
            AlgorithmRunner.GroupsRoom[GroupIndex] = new List<Room>();
            int OldRoomUsedIndex = RoomUsedIndex;
            while (StudentsNumber > 0)
            {
                RoomUsedIndex++;
                if (RoomUsedIndex < RoomList.Count) // còn phòng
                {

                    AlgorithmRunner.GroupsRoom[GroupIndex].Add(RoomList[RoomUsedIndex]);
                    StudentsNumber -= RoomList[RoomUsedIndex].Container;

                    // đáng lẽ code phân trực tiếp sv vào phòng ở đây, nhưng như vậy phòng ít phòng nhiều
                    // để đó sau này truy vấn lại môn này thi mấy phòng rồi chia sau!
                }
                else // hết phòng == > cần giãn ca
                {
                    AlgorithmRunner.GroupsRoom[GroupIndex].Clear(); // xoá mấy phòng lỡ thêm vào
                    Progressed[GroupIndex] = false; // cho nó trở lại trạng thái chưa xử lý
                    RoomUsedIndex = OldRoomUsedIndex;
                    // chuyển môn hiện tại qua ca tiếp theo
                    AlgorithmRunner.GroupsTime[GroupIndex] = IncTime(AlgorithmRunner.GroupsTime[GroupIndex], 1);
                    int CurrentColor = AlgorithmRunner.Colors[GroupIndex];
                    // Kiểm tra và tăng max
                    if (AlgorithmRunner.MaxColorTime[CurrentColor] < AlgorithmRunner.GroupsTime[GroupIndex])
                    {
                        // đổi max
                        AlgorithmRunner.MaxColorTime[CurrentColor] = AlgorithmRunner.GroupsTime[GroupIndex];
                        // chuyển các môn màu khác ở ca phía sau đi ra sau 1 ca, tránh tình trạng khác màu mà cùng ca
                        IncSubjectAfter(CurrentColor);
                    }
                    return; // thoát luôn
                }
            }
            MakeStudentList(GroupIndex);
        }
        /// <summary>
        /// chuyển các môn màu khác ở ca phía sau đi ra sau 1 ca, tránh tình trạng khác màu mà cùng ca
        /// </summary>
        /// <param name="CurrentColor"></param>
        private static void IncSubjectAfter(int CurrentColor)
        {
            if (CurrentColor < AlgorithmRunner.ColorNumber)
            {
                for (int si = 0; si < AlgorithmRunner.GroupsTime.Count(); si++)
                {
                    if (AlgorithmRunner.Colors[si] > CurrentColor)
                    {
                        AlgorithmRunner.GroupsTime[si] = IncTime(AlgorithmRunner.GroupsTime[si], 1);
                        if (AlgorithmRunner.MaxColorTime[AlgorithmRunner.Colors[si]] < AlgorithmRunner.GroupsTime[si])
                        {
                            // đổi max
                            AlgorithmRunner.MaxColorTime[AlgorithmRunner.Colors[si]] = AlgorithmRunner.GroupsTime[si];
                        }
                    }
                }
            }
        }

        private static List<int> GetNextShiftGroups()
        {
            List<int> Result = new List<int>();
            for (int i = 0; i < AlgorithmRunner.Groups.Count; i++)
            {
                if (!Progressed[i])
                {
                    DateTime ShiftTime = AlgorithmRunner.GroupsTime[i];
                    for (int si = 0; si < AlgorithmRunner.Groups.Count; si++)
                    {
                        if (!Progressed[si] && AlgorithmRunner.GroupsTime[si] == ShiftTime)
                        {
                            Progressed[si] = true; // xử lý nó rồi
                            Result.Add(si);
                        }
                    }
                    return Result;
                }
            }
            return Result;
        }
    }
}
