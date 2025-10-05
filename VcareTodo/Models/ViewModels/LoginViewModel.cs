using System.ComponentModel.DataAnnotations;

namespace VcareTodo.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "ユーザーIDを入力してください")]
        [Display(Name = "ユーザーID")]
        public string UserCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "パスワードを入力してください")]
        [Display(Name = "パスワード")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}