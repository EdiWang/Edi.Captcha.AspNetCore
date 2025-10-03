using Edi.Captcha.SampleApp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Edi.Captcha.SampleApp.Controllers;

public class SharedKeyStatelessController : Controller
{
    private readonly IStatelessCaptcha _captcha;

    public SharedKeyStatelessController(IEnumerable<IStatelessCaptcha> captchaServices)
    {
        // Get the last registered IStatelessCaptcha service, which should be SharedKeyStatelessCaptcha
        _captcha = captchaServices.Last();
    }

    public IActionResult Index()
    {
        return View(new SharedKeyStatelessHomeModel());
    }

    [HttpPost]
    public IActionResult Index(SharedKeyStatelessHomeModel model)
    {
        if (ModelState.IsValid)
        {
            bool isValidCaptcha = _captcha.Validate(model.CaptchaCode, model.CaptchaToken);
            return Content(isValidCaptcha ? "Success - Shared Key Stateless captcha validated!" : "Invalid captcha code");
        }

        return BadRequest();
    }

    [Route("get-shared-key-stateless-captcha")]
    public IActionResult GetSharedKeyStatelessCaptcha()
    {
        var result = _captcha.GenerateCaptcha(100, 36);

        return Json(new
        {
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