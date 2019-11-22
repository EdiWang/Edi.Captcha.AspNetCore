using System.ComponentModel.DataAnnotations;

namespace Edi.Captcha.SampleApp.Models
{
    public class HomeModel
    {
        [Required]
        [StringLength(4)]
        public string CaptchaCode { get; set; }
    }
}
