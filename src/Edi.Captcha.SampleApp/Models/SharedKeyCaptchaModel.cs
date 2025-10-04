using System.ComponentModel.DataAnnotations;

namespace Edi.Captcha.SampleApp.Models;

public class SharedKeyCaptchaModel : ICaptchableWithToken
{
    [Required]
    [StringLength(4)]
    public string CaptchaCode { get; set; }

    public string CaptchaToken { get; set; }
}