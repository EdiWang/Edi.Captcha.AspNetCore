using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace Edi.Captcha
{
    public class SessionBasedCaptchaOptions
    {
        public string SessionName { get; set; }
    }

    public abstract class SessionBasedCaptcha : ISessionBasedCaptcha
    {
        public SessionBasedCaptchaOptions Options { get; set; }

        public abstract string GenerateCaptchaCode();

        public byte[] GenerateCaptchaImageBytes(ISession httpSession, int width = 100, int height = 36)
        {
            EnsureHttpSession(httpSession);

            var captchaCode = GenerateCaptchaCode();
            var result = CaptchaImageGenerator.GetImage(width, height, captchaCode);
            httpSession.SetString(Options.SessionName, result.CaptchaCode);
            return result.CaptchaByteData;
        }

        public FileStreamResult GenerateCaptchaImageFileStream(ISession httpSession, int width = 100, int height = 36)
        {
            EnsureHttpSession(httpSession);

            var captchaCode = GenerateCaptchaCode();
            var result = CaptchaImageGenerator.GetImage(width, height, captchaCode);
            httpSession.SetString(Options.SessionName, result.CaptchaCode);
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
        public bool Validate(string userInputCaptcha, ISession httpSession, bool ignoreCase = true, bool dropSession = true)
        {
            if (string.IsNullOrWhiteSpace(userInputCaptcha))
            {
                return false;
            }
            var codeInSession = httpSession.GetString(Options.SessionName);
            var isValid = string.Compare(userInputCaptcha, codeInSession, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            if (dropSession)
            {
                httpSession.Remove(Options.SessionName);
            }
            return isValid == 0;
        }

        private static void EnsureHttpSession(ISession httpSession)
        {
            if (null == httpSession)
            {
                throw new ArgumentNullException(nameof(httpSession),
                    "Session can not be null, please check if Session is enabled in ASP.NET Core via services.AddSession() and app.UseSession().");
            }
        }
    }
}