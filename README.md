# Edi.Captcha.AspNetCore
The Captcha module used in my blog

[![.NET](https://github.com/EdiWang/Edi.Captcha.AspNetCore/actions/workflows/dotnet.yml/badge.svg)](https://github.com/EdiWang/Edi.Captcha.AspNetCore/actions/workflows/dotnet.yml)

[![NuGet][main-nuget-badge]][main-nuget]

[main-nuget]: https://www.nuget.org/packages/Edi.Captcha/
[main-nuget-badge]: https://img.shields.io/nuget/v/Edi.Captcha.svg?style=flat-square&label=nuget

## Install

NuGet Package Manager
```
Install-Package Edi.Captcha
```

or .NET CLI

```
dotnet add package Edi.Captcha
```

## Session-Based Captcha (Traditional Approach)

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
        HttpContext.Session,
        100,
        36
    );
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


## Stateless Captcha (Recommended for Scalable Applications)

**Advantages of Stateless Captcha:**
- ✅ Works in clustered/load-balanced environments
- ✅ No server-side session storage required
- ✅ Built-in expiration through encryption
- ✅ Secure token-based validation
- ✅ Better scalability
- ✅ Single API call for both token and image

### 1. Register in DI

```csharp
services.AddStatelessCaptcha();
```

or with custom options:

```csharp
services.AddStatelessCaptcha(options =>
{
    options.Letters = "2346789ABCDGHKMNPRUVWXYZ";
    options.CodeLength = 4;
    options.TokenExpiration = TimeSpan.FromMinutes(5);
});
```

### 2. Create Model with Token Support

```csharp
public class StatelessHomeModel
{
    [Required]
    [StringLength(4)]
    public string CaptchaCode { get; set; }
    
    public string CaptchaToken { get; set; }
}
```

### 3. Example Controller

```csharp
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
```

### 4. Example View

```razor
@model StatelessHomeModel
@{
    ViewData["Title"] = "Stateless Captcha Example";
}

<div class="text-center">
    <h1 class="display-4">Stateless Captcha Example</h1>
    <p>This example shows how to use stateless captcha that works in clustered environments.</p>
</div>

<div class="row">
    <div class="col-md-6 offset-md-3">
        <div class="card">
            <div class="card-header">
                <h5>Stateless Captcha Form</h5>
            </div>
            <div class="card-body">
                <form asp-action="Index" method="post" id="stateless-form">
                    <div class="form-group mb-3">
                        <label>Captcha Image:</label>
                        <div class="d-flex align-items-center">
                            <img id="captcha-image" src="" alt="Captcha" class="me-2" style="border: 1px solid #ccc;" />
                            <button type="button" class="btn btn-sm btn-outline-secondary" onclick="refreshCaptcha()">
                                🔄 Refresh
                            </button>
                        </div>
                        <small class="form-text text-muted">Click refresh to get a new captcha</small>
                    </div>

                    <div class="form-group mb-3">
                        <label asp-for="CaptchaCode">Enter Captcha Code:</label>
                        <input asp-for="CaptchaCode" class="form-control" placeholder="Enter the code from image" autocomplete="off" />
                        <span asp-validation-for="CaptchaCode" class="text-danger"></span>
                    </div>

                    <input type="hidden" asp-for="CaptchaToken" id="captcha-token" />

                    <div class="form-group">
                        <button type="submit" class="btn btn-primary">Submit</button>
                        <a asp-controller="Home" asp-action="Index" class="btn btn-secondary">Session-based Example</a>
                    </div>
                </form>

                <div class="mt-4">
                    <h6>Advantages of Stateless Captcha:</h6>
                    <ul class="small">
                        <li>✅ Works in clustered/load-balanced environments</li>
                        <li>✅ No server-side session storage required</li>
                        <li>✅ Built-in expiration through encryption</li>
                        <li>✅ Secure token-based validation</li>
                        <li>✅ Better scalability</li>
                        <li>✅ Single API call for both token and image</li>
                    </ul>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
    async function refreshCaptcha() {
        try {
            const response = await fetch('/get-stateless-captcha');
            const data = await response.json();
            
            // Set the token for validation
            document.getElementById('captcha-token').value = data.token;
            
            // Set the image source using base64 data
            document.getElementById('captcha-image').src = `data:image/png;base64,${data.imageBase64}`;
            
            // Clear the input
            document.getElementById('CaptchaCode').value = '';
        } catch (error) {
            console.error('Error refreshing captcha:', error);
            alert('Failed to load captcha. Please try again.');
        }
    }

    // Initialize captcha on page load
    document.addEventListener('DOMContentLoaded', function() {
        refreshCaptcha();
    });
</script>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
```

## Cluster/Load Balancer Configuration

⚠️ **Important for Production Deployments**: The stateless captcha uses ASP.NET Core's Data Protection API for token encryption. In clustered environments or behind load balancers, you **must** configure shared data protection keys to ensure captcha tokens can be validated on any server.

### Configure Shared Data Protection Keys

Choose one of the following approaches based on your infrastructure:

#### Option 1: File System (Network Share)
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(@"\\shared-network-path\keys"))
        .SetApplicationName("YourAppName"); // Must be consistent across all instances
    
    services.AddStatelessCaptcha(options =>
    {
        // Your captcha configuration
    });
}
```

#### Option 2: Azure Blob Storage
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDataProtection()
        .PersistKeysToAzureBlobStorage("DefaultEndpointsProtocol=https;AccountName=...", "keys-container", "dataprotection-keys.xml")
        .SetApplicationName("YourAppName");
    
    services.AddStatelessCaptcha(options =>
    {
        // Your captcha configuration
    });
}
```

#### Option 3: Redis
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect("your-redis-connection"), "DataProtection-Keys")
        .SetApplicationName("YourAppName");
    
    services.AddStatelessCaptcha(options =>
    {
        // Your captcha configuration
    });
}
```

#### Option 4: SQL Server
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDataProtection()
        .PersistKeysToDbContext<YourDbContext>()
        .SetApplicationName("YourAppName");
    
    services.AddStatelessCaptcha(options =>
    {
        // Your captcha configuration
    });
}
```

### Single Server Deployment

For single server deployments, no additional configuration is required. The default Data Protection configuration will work correctly.

### Testing Cluster Configuration

To verify your cluster configuration is working:

1. Generate a captcha on Server A
2. Submit the form to Server B (or any other server)
3. Validation should succeed

If validation fails with properly entered captcha codes, check your Data Protection configuration.

