using System;
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

        /// <summary>
        /// Validate Captcha Code
        /// </summary>
        /// <param name="userInputCaptcha">User Input Captcha Code</param>
        /// <param name="httpSession">Current Session</param>
        /// <param name="ignoreCase">Ignore Case (default = true)</param>
        /// <param name="dropSession">Whether to drop session regardless of the validation pass or not (default = true)</param>
        /// <returns>Is Valid Captcha Challenge</returns>
        public bool ValidateCaptchaCode(string userInputCaptcha, ISession httpSession, bool ignoreCase = true, bool dropSession = true)
        {
            var codeInSession = httpSession.GetString(SessionName);
            var isValid = string.Compare(userInputCaptcha, codeInSession, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (dropSession)
            {
                httpSession.Remove(SessionName);
            }
            return isValid == 0;
        }
    }
}