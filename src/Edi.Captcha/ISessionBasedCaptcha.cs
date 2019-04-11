using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Edi.Captcha
{
    public interface ISessionBasedCaptcha
    {
        FileStreamResult GenerateCaptchaImageFileStream(ISession httpSession, int width = 100, int height = 36);

        bool ValidateCaptchaCode(string userInputCaptcha, ISession httpSession, bool ignoreCase = true, bool dropSession = true);
    }
}