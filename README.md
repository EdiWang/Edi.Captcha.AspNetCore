# Edi.Captcha.AspNetCore
The Captcha module used in my blog

## Usage

### 1. Register in DI

```
services.AddTransient<ISessionBasedCaptcha, BasicLetterCaptcha>();
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