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

            // 未定タスクを取得
            var unscheduledQuery = @"
                SELECT ID, 内容, 取引先コード, 取引先名, 部署コード, 部署名, PLANID, PlanName,
                       SYSTEMID, SystemName, 対応方法, 見積工数, 実績工数, 保守工数,
                       起案者, 担当者, 分類, 備考, 状態, 起案日, 新規登録日
                FROM T_システム管理台帳
                WHERE 担当者 = @UserCode AND 作業予定日 IS NULL AND (状態 != '完了' OR 状態 IS NULL)
                ORDER BY 新規登録日 DESC";

            viewModel.UnscheduledTasks = (await _db.QueryAsync<TodoTask>(unscheduledQuery, new { UserCode = targetUserCode })).ToList();

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
                       作業予定日, 修正完了日, 起案者, 担当者, 分類, 備考, 状態, 起案日, 新規登録日
                FROM T_システム管理台帳
                WHERE 担当者 = @UserCode AND 作業予定日 >= @StartDate AND 作業予定日 < @EndDate
                ORDER BY 作業予定日, 新規登録日";

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
                     作業予定日, 修正完了日, 起案者, 担当者, 分類, 備考, 新規登録日)
                    VALUES
                    (@内容, @取引先コード, @取引先名, @部署コード, @部署名, @PLANID, @PlanName,
                     @SYSTEMID, @SystemName, @対応方法, @見積工数, @実績工数, @保守工数,
                     @作業予定日, @修正完了日, @起案者, @担当者, @分類, @備考, GETDATE())";

                if (string.IsNullOrEmpty(task.担当者))
                {
                    task.担当者 = currentUserCode;
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
                var query = @"
                    UPDATE T_システム管理台帳
                    SET 内容 = @内容, 取引先コード = @取引先コード, 取引先名 = @取引先名,
                        部署コード = @部署コード, 部署名 = @部署名, PLANID = @PLANID, PlanName = @PlanName,
                        SYSTEMID = @SYSTEMID, SystemName = @SystemName, 対応方法 = @対応方法,
                        見積工数 = @見積工数, 実績工数 = @実績工数, 保守工数 = @保守工数,
                        作業予定日 = @作業予定日, 修正完了日 = @修正完了日, 起案者 = @起案者,
                        担当者 = @担当者, 分類 = @分類, 備考 = @備考
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
                var query = "UPDATE T_システム管理台帳 SET 作業予定日 = @Date WHERE ID = @TaskId";
                await _db.ExecuteAsync(query, new { TaskId = request.TaskId, Date = request.Date });
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
                           作業予定日, 修正完了日, 起案者, 担当者, 分類, 備考, 状態, 起案日, 新規登録日
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
    }

    public class DeleteTaskRequest
    {
        public decimal TaskId { get; set; }
    }
}