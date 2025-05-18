using ClientWebApp.Data;
using ClientWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ClientWebApp.ActionFilters
{
    public class DownloadActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                var request = context.ActionArguments["request"] as DownloadFileDTO;

                if (request == null ||
                    string.IsNullOrEmpty(request.Email) ||
                    string.IsNullOrEmpty(request.AccessCode))
                {
                    context.Result = new BadRequestResult();
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();

                var permission = dbContext.AccessPermissions
               .Include(p => p.UploadedFile)
               .FirstOrDefault(p =>
                   p.LawyerEmail == request.Email &&
                   p.AccessCode == request.AccessCode);


                if (permission == null || permission.IsDownloaded)
                {
                    context.Result = new ObjectResult("You do not have access or the file has already been downloaded.")
                    {
                        StatusCode = 403
                    };
                    return;
                }

                context.HttpContext.Items["permission"] = permission;
            }
            catch (Exception)
            {
                context.Result = new ForbidResult();
            }

            base.OnActionExecuting(context);
        }
    }
}
