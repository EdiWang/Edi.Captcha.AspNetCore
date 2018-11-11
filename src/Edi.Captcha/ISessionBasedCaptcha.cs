using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Edi.Captcha
{
    public interface ISessionBasedCaptcha
    {
        FileStreamResult GenerateCaptchaImageFileStream(int width, int height, ISession httpSession);

        bool ValidateCaptchaCode(string userInputCaptcha, ISession httpSession);
    }
}