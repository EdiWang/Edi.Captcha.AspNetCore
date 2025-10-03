using System.ComponentModel.DataAnnotations;

namespace Edi.Captcha.SampleApp.Models;

public class SessionCaptchaModel
{
    [Required]
    [StringLength(4)]
    public string CaptchaCode { get; set; }
}