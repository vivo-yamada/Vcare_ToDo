using Microsoft.AspNetCore.Mvc;
using VcareTodo.Services;
using System.Text;

namespace VcareTodo.Controllers
{
    public class AdminController : Controller
    {
        private readonly AuthenticationService _authService;

        public AdminController(AuthenticationService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// 管理者用ユーザー作成画面
        /// http://localhost:5000/Admin/CreateUser
        /// </summary>
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string userCode, string password)
        {
            if (string.IsNullOrEmpty(userCode) || string.IsNullOrEmpty(password))
            {
                ViewBag.Message = "ユーザーコードとパスワードを入力してください。";
                return View();
            }

            bool success = await _authService.CreateUserAuthenticationAsync(userCode, password);

            if (success)
            {
                ViewBag.Message = $"ユーザー '{userCode}' の認証データを作成しました。";
                ViewBag.Success = true;
            }
            else
            {
                ViewBag.Message = $"ユーザー '{userCode}' の認証データ作成に失敗しました。（既に存在するか、T_社員テーブルにユーザーが存在しません）";
                ViewBag.Success = false;
            }

            return View();
        }

        /// <summary>
        /// password123のハッシュ値を生成して表示
        /// http://localhost:5000/Admin/GenerateHash
        /// </summary>
        public IActionResult GenerateHash()
        {
            string password = "password123";
            string salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);

            ViewBag.Password = password;
            ViewBag.Salt = salt;
            ViewBag.Hash = hashedPassword;
            ViewBag.SqlUpdate = $"UPDATE T_ユーザー認証 SET パスワードハッシュ = '{hashedPassword}', ソルト = '{salt}' WHERE ユーザーコード = '0002';";
            ViewBag.SqlInsert = $"INSERT INTO T_ユーザー認証 (ユーザーコード, パスワードハッシュ, ソルト) VALUES ('0002', '{hashedPassword}', '{salt}');";

            return View();
        }
    }
}