# Edi.Captcha.AspNetCore
The Captcha module used in my blog

[![Build status](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_apis/build/status/Edi.Captcha.AspNetCore-CI)](https://dev.azure.com/ediwang/EdiWang-GitHub-Builds/_build/latest?definitionId=42)

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

### 2. Use DI in Controller

```
private readonly ISessionBasedCaptcha _captcha;

public SomeController(ISessionBasedCaptcha captcha)
{
    _captcha = captcha;
}

```

### 3. Add an Action to return captcha image

```
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

### 4. Add CaptchaCode Property to Model

```
[Required]
[StringLength(4)]
public string CaptchaCode { get; set; }
```

### 5. View

```
<div class="col">
    <div class="input-group">
        <div class="input-group-prepend">
            <img id="img-captcha" src="~/get-captcha-image" />
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

```
_captcha.ValidateCaptchaCode(model.CommentPostModel.CaptchaCode, HttpContext.Session)
```

Refer to https://edi.wang/post/2018/10/13/generate-captcha-code-aspnet-core
