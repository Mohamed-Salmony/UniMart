using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using System.Threading.Tasks;

namespace UniMart_App.Helpers
{
    public static class ControllerExtensions
    {
        public static async Task<string> RenderViewToStringAsync(this Controller controller, string viewPath, object model)
        {
            controller.ViewData.Model = model;
            using (var writer = new StringWriter())
            {
                var viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                var viewResult = viewEngine.FindView(controller.ControllerContext, viewPath, false);
                if (!viewResult.Success)
                {
                    viewResult = viewEngine.GetView(null, viewPath, false);
                }
                if (!viewResult.Success)
                {
                    throw new FileNotFoundException($"View {viewPath} not found.");
                }
                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );
                await viewResult.View.RenderAsync(viewContext);
                return writer.GetStringBuilder().ToString();
            }
        }
    }
}
