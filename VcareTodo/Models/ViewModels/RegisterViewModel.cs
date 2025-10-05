using System.ComponentModel.DataAnnotations;

namespace VcareTodo.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "ユーザーIDを入力してください")]
        [Display(Name = "ユーザーID")]
        [StringLength(8, ErrorMessage = "ユーザーIDは8文字以内で入力してください")]
        public string UserCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "お名前を入力してください")]
        [Display(Name = "お名前")]
        [StringLength(45, ErrorMessage = "お名前は45文字以内で入力してください")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "メールアドレス")]
        [EmailAddress(ErrorMessage = "正しいメールアドレスを入力してください")]
        [StringLength(100, ErrorMessage = "メールアドレスは100文字以内で入力してください")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "パスワードを入力してください")]
        [Display(Name = "パスワード")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "パスワードは6文字以上100文字以内で入力してください")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "パスワード確認を入力してください")]
        [Display(Name = "パスワード確認")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "パスワードとパスワード確認が一致しません")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}