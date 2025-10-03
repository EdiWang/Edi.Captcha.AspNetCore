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

### 3. Example Controller and View

See: [src\Edi.Captcha.SampleApp\Controllers\StatelessController.cs](src/Edi.Captcha.SampleApp/Controllers/StatelessController.cs) and [src\Edi.Captcha.SampleApp\Views\Stateless\Index.cshtml](src/Edi.Captcha.SampleApp/Views/Stateless/Index.cshtml) for a complete example.

### Cluster/Load Balancer Configuration

⚠️ **Important for Production Deployments**: The stateless captcha uses ASP.NET Core's Data Protection API for token encryption. In clustered environments or behind load balancers, you **must** configure shared data protection keys to ensure captcha tokens can be validated on any server.

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

#### Single Server Deployment

For single server deployments, no additional configuration is required. The default Data Protection configuration will work correctly.

#### Testing Cluster Configuration

To verify your cluster configuration is working:

1. Generate a captcha on Server A
2. Submit the form to Server B (or any other server)
3. Validation should succeed

If validation fails with properly entered captcha codes, check your Data Protection configuration.

## Shared Key Stateless Captcha (Recommended for Scalable Applications without DPAPI)

**When to use Shared Key Stateless Captcha:**
- ✅ Full control over encryption keys
- ✅ Works without ASP.NET Core Data Protection API
- ✅ Simpler cluster configuration
- ✅ Custom key rotation strategies
- ✅ Works across different application frameworks
- ✅ No dependency on external storage for keys

### 1. Register in DI with Shared Key

```csharp
services.AddSharedKeyStatelessCaptcha(options =>
{
    options.SharedKey = "your-32-byte-base64-encoded-key"; // Generate securely
    options.FontStyle = FontStyle.Bold;
    options.DrawLines = true;
    options.TokenExpiration = TimeSpan.FromMinutes(5);
});
```

### 2. Generate Secure Shared Key

**Important**: Use a cryptographically secure random key. Here's how to generate one:

```csharp
// Generate a secure 256-bit key (one-time setup)
using (var rng = RandomNumberGenerator.Create())
{
    var keyBytes = new byte[32]; // 256 bits
    rng.GetBytes(keyBytes);
    var base64Key = Convert.ToBase64String(keyBytes);
    Console.WriteLine($"Shared Key: {base64Key}");
}
```

### 3. Configuration Options

#### Configuration File (appsettings.json)
```json
{
  "CaptchaSettings": {
    "SharedKey": "your-generated-base64-key-here",
    "TokenExpirationMinutes": 5
  }
}
```

```csharp
public void ConfigureServices(IServiceCollection services)
{
    var captchaKey = Configuration["CaptchaSettings:SharedKey"];
    var expirationMinutes = Configuration.GetValue<int>("CaptchaSettings:TokenExpirationMinutes", 5);
    
    services.AddSharedKeyStatelessCaptcha(options =>
    {
        options.SharedKey = captchaKey;
        options.TokenExpiration = TimeSpan.FromMinutes(expirationMinutes);
        // Other options...
    });
}
```

### 4. Example Controller and View

See: [src\Edi.Captcha.SampleApp\Controllers\SharedKeyStatelessController.cs](src/Edi.Captcha.SampleApp/Controllers/SharedKeyStatelessController.cs) and [src\Edi.Captcha.SampleApp\Views\SharedKeyStateless\Index.cshtml](src/Edi.Captcha.SampleApp/Views/SharedKeyStateless/Index.cshtml) for a complete example.