using Microsoft.AspNetCore.Mvc;
using VcareTodo.Data;
using VcareTodo.Models;
using VcareTodo.Models.ViewModels;
using Newtonsoft.Json;

namespace VcareTodo.Controllers
{
    public class TodoController : Controller
    {
        private readonly DatabaseContext _db;

        public TodoController(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? userCode = null, string viewMode = "week", DateTime? date = null)
        {
            var currentUserCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(currentUserCode))
            {
                return RedirectToAction("Login", "Account");
            }

            var targetUserCode = userCode ?? currentUserCode;
            var currentDate = date ?? DateTime.Today;

            var viewModel = new TodoViewModel
            {
                CurrentUserCode = currentUserCode,
                CurrentUserName = HttpContext.Session.GetString("UserName") ?? "",
                SelectedUserCode = targetUserCode,
                ViewMode = viewMode,
                CurrentDate = currentDate
            };

            // 未定タスクを取得（担当者コード列を使用）
            var unscheduledQuery = @"
                SELECT ID, 内容, 取引先コード, 取引先名, 部署コード, 部署名, PLANID, PlanName,
                       SYSTEMID, SystemName, 対応方法, 見積工数, 実績工数, 保守工数,
                       作業予定日, 開始時刻, 終了時刻, 修正完了日,
                       起案者, 担当者, 担当者コード, 分類, 備考, 状態, 起案日, 新規登録日, 重要タスクFLG
                FROM T_システム管理台帳
                WHERE 担当者コード = @UserCode AND 作業予定日 IS NULL AND (状態 != '完了' OR 状態 IS NULL)
                ORDER BY 新規登録日 DESC";

            Console.WriteLine($"[DEBUG] UnscheduledTasks Query - UserCode: {targetUserCode}");
            var unscheduledTasks = (await _db.QueryAsync<TodoTask>(unscheduledQuery, new { UserCode = targetUserCode })).ToList();
            Console.WriteLine($"[DEBUG] UnscheduledTasks Count: {unscheduledTasks.Count}");
            viewModel.UnscheduledTasks = unscheduledTasks;

            // 予定タスクを取得（表示期間に応じて）
            DateTime startDate, endDate;
            switch (viewMode)
            {
                case "day":
                    startDate = currentDate.Date;
                    endDate = currentDate.Date.AddDays(1);
                    break;
                case "month":
                    startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                    endDate = startDate.AddMonths(1);
                    break;
                default: // week
                    var dayOfWeek = (int)currentDate.DayOfWeek;
                    startDate = currentDate.AddDays(-dayOfWeek);
                    endDate = startDate.AddDays(7);
                    break;
            }

            var scheduledQuery = @"
                SELECT ID, 内容, 取引先コード, 取引先名, 部署コード, 部署名, PLANID, PlanName,
                       SYSTEMID, SystemName, 対応方法, 見積工数, 実績工数, 保守工数,
                       作業予定日, 開始時刻, 終了時刻, 修正完了日, 起案者, 担当者, 担当者コード, 分類, 備考, 状態, 起案日, 新規登録日, 重要タスクFLG
                FROM T_システム管理台帳
                WHERE 担当者コード = @UserCode AND 作業予定日 >= @StartDate AND 作業予定日 < @EndDate
                ORDER BY 作業予定日, 開始時刻, 新規登録日";

            viewModel.ScheduledTasks = (await _db.QueryAsync<TodoTask>(scheduledQuery, new { UserCode = targetUserCode, StartDate = startDate, EndDate = endDate })).ToList();

            // 社員一覧を取得
            var employeeQuery = "SELECT コード, 氏名 FROM T_社員 ORDER BY コード";
            viewModel.Employees = (await _db.QueryAsync<Employee>(employeeQuery)).ToList();

            // 作業分類を取得
            var workCategoryQuery = "SELECT 対応方法 FROM T_作業区分 ORDER BY 対応方法";
            viewModel.WorkCategories = (await _db.QueryAsync<WorkCategory>(workCategoryQuery)).ToList();

            // 報告区分を取得
            var reportCategoryQuery = @"
                SELECT 分類名, 分類コード, 備考, 具体例, 順番
                FROM T_分類マスタ
                WHERE 有効FLG = -1
                ORDER BY 順番";
            viewModel.ReportCategories = (await _db.QueryAsync<ReportCategory>(reportCategoryQuery)).ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TodoTask task)
        {
            var currentUserCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(currentUserCode))
            {
                return Json(new { success = false, message = "ログインが必要です" });
            }

            try
            {
                var query = @"
                    INSERT INTO T_システム管理台帳
                    (内容, 取引先コード, 取引先名, 部署コード, 部署名, PLANID, PlanName,
                     SYSTEMID, SystemName, 対応方法, 見積工数, 実績工数, 保守工数,
                     作業予定日, 開始時刻, 終了時刻, 修正完了日, 起案者, 担当者, 担当者コード, 分類, 備考, 新規登録日, 重要タスクFLG)
                    VALUES
                    (@内容, @取引先コード, @取引先名, @部署コード, @部署名, @PLANID, @PlanName,
                     @SYSTEMID, @SystemName, @対応方法, @見積工数, @実績工数, @保守工数,
                     @作業予定日, @開始時刻, @終了時刻, @修正完了日, @起案者, @担当者, @担当者コード, @分類, @備考, GETDATE(), @重要タスクFLG)";

                if (string.IsNullOrEmpty(task.担当者コード))
                {
                    task.担当者コード = currentUserCode;
                    // 担当者コードから担当者名を取得
                    var employeeName = await _db.QueryFirstOrDefaultAsync<string>(
                        "SELECT 氏名 FROM T_社員 WHERE コード = @UserCode",
                        new { UserCode = currentUserCode });
                    task.担当者 = employeeName;
                }

                await _db.ExecuteAsync(query, task);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTask([FromBody] TodoTask task)
        {
            var currentUserCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(currentUserCode))
            {
                return Json(new { success = false, message = "ログインが必要です" });
            }

            try
            {
                // 担当者コードが変更された場合、担当者名も更新
                if (!string.IsNullOrEmpty(task.担当者コード))
                {
                    var employeeName = await _db.QueryFirstOrDefaultAsync<string>(
                        "SELECT 氏名 FROM T_社員 WHERE コード = @UserCode",
                        new { UserCode = task.担当者コード });
                    task.担当者 = employeeName;
                }

                var query = @"
                    UPDATE T_システム管理台帳
                    SET 内容 = @内容, 取引先コード = @取引先コード, 取引先名 = @取引先名,
                        部署コード = @部署コード, 部署名 = @部署名, PLANID = @PLANID, PlanName = @PlanName,
                        SYSTEMID = @SYSTEMID, SystemName = @SystemName, 対応方法 = @対応方法,
                        見積工数 = @見積工数, 実績工数 = @実績工数, 保守工数 = @保守工数,
                        作業予定日 = @作業予定日, 開始時刻 = @開始時刻, 終了時刻 = @終了時刻, 修正完了日 = @修正完了日, 起案者 = @起案者,
                        担当者 = @担当者, 担当者コード = @担当者コード, 分類 = @分類, 備考 = @備考, 重要タスクFLG = @重要タスクFLG
                    WHERE ID = @ID";

                await _db.ExecuteAsync(query, task);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTaskDate([FromBody] UpdateTaskDateRequest request)
        {
            var currentUserCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(currentUserCode))
            {
                return Json(new { success = false, message = "ログインが必要です" });
            }

            try
            {
                string query;
                if (request.Date == null)
                {
                    // 未定に戻す場合は新規登録日を更新して一番上に表示
                    query = "UPDATE T_システム管理台帳 SET 作業予定日 = NULL, 開始時刻 = NULL, 終了時刻 = NULL, 新規登録日 = GETDATE() WHERE ID = @TaskId";
                    await _db.ExecuteAsync(query, new { TaskId = request.TaskId });
                }
                else
                {
                    query = "UPDATE T_システム管理台帳 SET 作業予定日 = @Date, 開始時刻 = @StartTime, 終了時刻 = @EndTime WHERE ID = @TaskId";
                    await _db.ExecuteAsync(query, new {
                        TaskId = request.TaskId,
                        Date = request.Date,
                        StartTime = request.StartTime,
                        EndTime = request.EndTime
                    });
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTask([FromBody] DeleteTaskRequest request)
        {
            var currentUserCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(currentUserCode))
            {
                return Json(new { success = false, message = "ログインが必要です" });
            }

            try
            {
                var query = "DELETE FROM T_システム管理台帳 WHERE ID = @TaskId";
                await _db.ExecuteAsync(query, new { TaskId = request.TaskId });
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTask(decimal id)
        {
            var currentUserCode = HttpContext.Session.GetString("UserCode");
            if (string.IsNullOrEmpty(currentUserCode))
            {
                return Json(new { success = false, message = "ログインが必要です" });
            }

            try
            {
                var query = @"
                    SELECT ID, 内容, 取引先コード, 取引先名, 部署コード, 部署名, PLANID, PlanName,
                           SYSTEMID, SystemName, 対応方法, 見積工数, 実績工数, 保守工数,
                           作業予定日, 開始時刻, 終了時刻, 修正完了日, 起案者, 担当者, 担当者コード, 分類, 備考, 状態, 起案日, 新規登録日, 重要タスクFLG
                    FROM T_システム管理台帳
                    WHERE ID = @Id";

                var task = await _db.QueryFirstOrDefaultAsync<TodoTask>(query, new { Id = id });
                if (task == null)
                {
                    return Json(new { success = false, message = "タスクが見つかりません" });
                }

                return Json(new { success = true, task });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class UpdateTaskDateRequest
    {
        public decimal TaskId { get; set; }
        public DateTime? Date { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }

    public class DeleteTaskRequest
    {
        public decimal TaskId { get; set; }
    }
}