using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Edi.Captcha.SampleApp.Models
{
    public class HomeModel
    {
        [Required]
        [StringLength(4)]
        public string CaptchaCode { get; set; }
    }
}
