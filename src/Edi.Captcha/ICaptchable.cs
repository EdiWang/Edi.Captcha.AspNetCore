using System;
using System.Collections.Generic;
using System.Text;

namespace Edi.Captcha
{
    public interface ICaptchable
    {
        string CaptchaCode { get; set; }
    }
}
