using System.Net;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddOutputCache(bld =>
{
    bld.AddBasePolicy(opt =>
    {
        opt.Expire(TimeSpan.FromSeconds(60));
    });
});

builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress;
        return RateLimitPartition.GetFixedWindowLimiter(ip!, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromSeconds(5),
            PermitLimit = 5,
            QueueLimit = 5
        });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use((ctx, next) =>
{
    ctx.Response.Cookies.Append("Test", "Value");
    return next.Invoke();
});

app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthorization();

app.UseOutputCache();

app.MapRazorPages();

app.Run();
