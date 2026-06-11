using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace WebPhotocopyHub.Web.Controllers;

[AllowAnonymous]
public sealed class DevOpenController : Controller
{
    [HttpGet("/dev/open")]
    [IgnoreAntiforgeryToken]
    public ContentResult Open()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var swaggerUrl = $"{baseUrl}/swagger";
        var homeUrl = $"{baseUrl}/Home";
        var shopsUrl = $"{baseUrl}/Shops";
        var pingUrl = $"{baseUrl}/api/v1/system/ping";

        var html = $$"""
<!doctype html>
<html lang="vi">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>WebPhotocopyHub - Dev Open</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #f7f7fb; color: #1f2937; }
        .card { max-width: 860px; background: #fff; border-radius: 16px; padding: 28px; box-shadow: 0 10px 30px rgba(0,0,0,.08); }
        h1 { margin-top: 0; }
        .actions { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 20px; }
        a, button { display: inline-block; padding: 12px 16px; border-radius: 10px; text-decoration: none; font-weight: 700; border: 0; cursor: pointer; font-size: 15px; }
        .primary { background: #2563eb; color: white; }
        .secondary { background: #e5e7eb; color: #111827; }
        code { background: #f3f4f6; padding: 2px 6px; border-radius: 6px; }
        .muted { color: #6b7280; }
        .list { display: grid; gap: 8px; margin-top: 18px; }
    </style>
</head>
<body>
    <div class="card">
        <h1>WebPhotocopyHub đang chạy</h1>
        <p>Trang này dùng để mở nhanh đúng 3 màn hình dev cần kiểm tra.</p>

        <div class="actions">
            <a class="primary" href="{{swaggerUrl}}" target="_blank" rel="noopener">Mở Swagger</a>
            <a class="primary" href="{{homeUrl}}" target="_blank" rel="noopener">Mở Local /Home</a>
            <a class="primary" href="{{shopsUrl}}" target="_blank" rel="noopener">Mở danh sách cơ sở</a>
            <button class="secondary" type="button" onclick="openAll()">Mở cả 3 trang</button>
        </div>

        <div class="list">
            <p class="muted">Swagger: <code>{{swaggerUrl}}</code></p>
            <p class="muted">Local /Home: <code>{{homeUrl}}</code></p>
            <p class="muted">Tất cả cơ sở: <code>{{shopsUrl}}</code></p>
            <p class="muted">API test: <code>{{pingUrl}}</code></p>
        </div>
    </div>

    <script>
        function openAll() {
            window.open('{{swaggerUrl}}', '_blank', 'noopener');
            window.open('{{homeUrl}}', '_blank', 'noopener');
            window.open('{{shopsUrl}}', '_blank', 'noopener');
        }
    </script>
</body>
</html>
""";

        return Content(html, "text/html", Encoding.UTF8);
    }
}