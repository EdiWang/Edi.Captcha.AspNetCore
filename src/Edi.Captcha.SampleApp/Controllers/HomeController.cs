using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Edi.Captcha.SampleApp.Models;

namespace Edi.Captcha.SampleApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISessionBasedCaptcha _captcha;

        public HomeController(ISessionBasedCaptcha captcha)
        {
            _captcha = captcha;
        }

        public IActionResult Index()
        {
            return View(new HomeModel());
        }

        [HttpPost]
        public IActionResult Index(HomeModel model)
        {
            if (ModelState.IsValid)
            {
                bool isValidCaptcha = _captcha.ValidateCaptchaCode(model.CaptchaCode, HttpContext.Session);
                return Content(isValidCaptcha ? "Success" : "Invalid captcha code");
            }

            return BadRequest();
        }

        [Route("get-captcha-image")]
        public IActionResult GetCaptchaImage()
        {
            var s = _captcha.GenerateCaptchaImageFileStream(HttpContext.Session);
            return s;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
