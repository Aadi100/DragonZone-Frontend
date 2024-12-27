namespace POS.Services
{
    public class RedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public RedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestPath = context.Request.Path.ToString();

            if (requestPath.StartsWith("/updatemember/") && requestPath != "/updatemember")
            {
                var memberValue = requestPath.Substring("/updatemember/".Length);

                context.Response.Cookies.Append("MemberValue", memberValue, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, 
                    SameSite = SameSiteMode.Strict
                });
                context.Response.Redirect("/updatemember");
                return;
            }

            await _next(context);
        }
    }
}
