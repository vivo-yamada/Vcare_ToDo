using Microsoft.AspNetCore.Mvc;
using VcareTodo.Data;
using VcareTodo.Models;
using VcareTodo.Models.ViewModels;
using VcareTodo.Services;

namespace VcareTodo.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseContext _db;
        private readonly AuthenticationService _authService;

        public AccountController(DatabaseContext db, AuthenticationService authService)
        {
            _db = db;
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, message, employee) = await _authService.AuthenticateAsync(model.UserCode, model.Password);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            HttpContext.Session.SetString("UserCode", employee!.コード);
            HttpContext.Session.SetString("UserName", employee.氏名 ?? "");

            return RedirectToAction("Index", "Todo");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var employee = await _db.QueryFirstOrDefaultAsync<Employee>(
                    "SELECT * FROM T_社員 WHERE コード = @UserCode",
                    new { UserCode = model.UserCode });

                if (employee == null)
                {
                    ModelState.AddModelError(string.Empty, "指定されたユーザーIDは存在しません");
                    return View(model);
                }

                var existingAuth = await _db.QueryFirstOrDefaultAsync<UserAuthentication>(
                    "SELECT * FROM T_ユーザー認証 WHERE ユーザーコード = @UserCode",
                    new { UserCode = model.UserCode });

                if (existingAuth != null)
                {
                    ModelState.AddModelError(string.Empty, "このユーザーIDは既に登録されています");
                    return View(model);
                }

                var (hashedPassword, salt) = _authService.HashPassword(model.Password);

                await _db.ExecuteAsync(@"
                    INSERT INTO T_ユーザー認証 (
                        ユーザーコード,
                        パスワードハッシュ,
                        ソルト,
                        ユーザー名,
                        メールアドレス,
                        作成日時,
                        更新日時,
                        失敗回数,
                        ロック状態,
                        最終ログイン日時
                    ) VALUES (
                        @UserCode,
                        @PasswordHash,
                        @Salt,
                        @Name,
                        @Email,
                        GETDATE(),
                        GETDATE(),
                        0,
                        0,
                        NULL
                    )",
                    new
                    {
                        UserCode = model.UserCode,
                        PasswordHash = hashedPassword,
                        Salt = salt,
                        Name = model.Name,
                        Email = model.Email
                    });

                TempData["Message"] = "アカウントが正常に作成されました。ログインしてください。";
                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "アカウント作成中にエラーが発生しました");
                return View(model);
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}