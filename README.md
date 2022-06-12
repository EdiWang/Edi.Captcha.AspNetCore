# Edi.Captcha.AspNetCore
The Captcha module used in my blog

[![.NET](https://github.com/EdiWang/Edi.Captcha.AspNetCore/actions/workflows/dotnet.yml/badge.svg)](https://github.com/EdiWang/Edi.Captcha.AspNetCore/actions/workflows/dotnet.yml)

[![NuGet][main-nuget-badge]][main-nuget]

[main-nuget]: https://www.nuget.org/packages/Edi.Captcha/
[main-nuget-badge]: https://img.shields.io/nuget/v/Edi.Captcha.svg?style=flat-square&label=nuget

## Usage

### 0. Install from NuGet

NuGet Package Manager
```
Install-Package Edi.Captcha
```

or .NET CLI

```
dotnet add package Edi.Captcha
```

### 1. Register in DI

```csharp
services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
});

services.AddSessionBasedCaptcha();
```

```csharp
// Don't forget to add this line in your `Configure` method.
 app.UseSession();
```

or you can customize the options

```csharp
services.AddSessionBasedCaptcha(option =>
{
    option.Letters = "2346789ABCDEFGHJKLMNPRTUVWXYZ";
    option.SessionName = "CaptchaCode";
    option.CodeLength = 4;
});
```

### 2. Generate Image

#### Using MVC Controller

```csharp
private readonly ISessionBasedCaptcha _captcha;

public SomeController(ISessionBasedCaptcha captcha)
{
    _captcha = captcha;
}

[Route("get-captcha-image")]
public IActionResult GetCaptchaImage()
{
    var s = _captcha.GenerateCaptchaImageFileStream(
        100,
        36,
        HttpContext.Session);
    return s;
}
```

#### Using Middleware

```csharp
app.UseSession().UseCaptchaImage(options =>
{
    options.RequestPath = "/captcha-image";
    options.ImageHeight = 36;
    options.ImageWidth = 100;
});
```

### 3. Add CaptchaCode Property to Model

```csharp
[Required]
[StringLength(4)]
public string CaptchaCode { get; set; }
```

### 5. View

```html
<div class="col">
    <div class="input-group">
        <div class="input-group-prepend">
            <img id="img-captcha" src="~/captcha-image" />
        </div>
        <input type="text" 
               asp-for="CommentPostModel.CaptchaCode" 
               class="form-control" 
               placeholder="Captcha Code" 
               autocomplete="off" 
               minlength="4"
               maxlength="4" />
    </div>
    <span asp-validation-for="CommentPostModel.CaptchaCode" class="text-danger"></span>
</div>
```

### 6. Validate Input

```csharp
_captcha.ValidateCaptchaCode(model.CommentPostModel.CaptchaCode, HttpContext.Session)
```

To make your code look more cool, you can also write an Action Filter like this:

```csharp
public class ValidateCaptcha : ActionFilterAttribute
{
    private readonly ISessionBasedCaptcha _captcha;

    public ValidateCaptcha(ISessionBasedCaptcha captcha)
    {
        _captcha = captcha;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var captchaedModel =
            context.ActionArguments.Where(p => p.Value is ICaptchable)
                                   .Select(x => x.Value as ICaptchable)
                                   .FirstOrDefault();

        if (null == captchaedModel)
        {
            context.ModelState.AddModelError(nameof(captchaedModel.CaptchaCode), "Captcha Code is required");
            context.Result = new BadRequestObjectResult(context.ModelState);
        }
        else
        {
            if (!_captcha.Validate(captchaedModel.CaptchaCode, context.HttpContext.Session))
            {
                context.ModelState.AddModelError(nameof(captchaedModel.CaptchaCode), "Wrong Captcha Code");
                context.Result = new ConflictObjectResult(context.ModelState);
            }
            else
            {
                base.OnActionExecuting(context);
            }
        }
    }
}
```

and then

```csharp
services.AddScoped<ValidateCaptcha>();
```

and then

```csharp

public class YourModelWithCaptchaCode : ICaptchable
{
    public string YourProperty { get; set; }

    [Required]
    [StringLength(4)]
    public string CaptchaCode { get; set; }
}

[ServiceFilter(typeof(ValidateCaptcha))]
public async Task<IActionResult> SomeAction(YourModelWithCaptchaCode model)
{
    // ....
}
```

Refer to https://edi.wang/post/2018/10/13/generate-captcha-code-aspnet-core
