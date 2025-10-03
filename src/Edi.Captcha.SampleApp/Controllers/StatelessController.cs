using Edi.Captcha.SampleApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;

namespace Edi.Captcha.SampleApp.Controllers;

public class StatelessController(IStatelessCaptcha captcha) : Controller
{
    public IActionResult Index()
    {
        return View(new StatelessHomeModel());
    }

    [HttpPost]
    public IActionResult Index(StatelessHomeModel model)
    {
        if (ModelState.IsValid)
        {
            bool isValidCaptcha = captcha.Validate(model.CaptchaCode, model.CaptchaToken);
            return Content(isValidCaptcha ? "Success - Stateless captcha validated!" : "Invalid captcha code");
        }

        return BadRequest();
    }

    [Route("get-stateless-captcha")]
    public IActionResult GetStatelessCaptcha()
    {
        var result = captcha.GenerateCaptcha(100, 36);
        
        return Json(new { 
            token = result.Token, 
            imageBase64 = Convert.ToBase64String(result.ImageBytes)
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}