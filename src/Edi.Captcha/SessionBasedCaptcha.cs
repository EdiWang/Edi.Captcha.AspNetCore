using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Edi.Captcha
{
    public abstract class SessionBasedCaptcha : ISessionBasedCaptcha
    {
        public string SessionName { get; set; }

        public abstract string GenerateCaptchaCode();

        public FileStreamResult GenerateCaptchaImageFileStream(int width, int height, ISession httpSession)
        {
            var captchaCode = GenerateCaptchaCode();
            var result = CaptchaImageGenerator.GetImage(width, height, captchaCode);
            httpSession.SetString(SessionName, result.CaptchaCode);
            Stream s = new MemoryStream(result.CaptchaByteData);
            return new FileStreamResult(s, "image/png");
        }

        public bool ValidateCaptchaCode(string userInputCaptcha, ISession httpSession)
        {
            var isValid = userInputCaptcha == httpSession.GetString(SessionName);
            httpSession.Remove(SessionName);
            return isValid;
        }
    }
}