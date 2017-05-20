using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace WebApplication5
{
    public class MvcApplication : System.Web.HttpApplication
    {

        private static void RegisterArea<T>(RouteCollection routes, object state) where T : AreaRegistration
        {
            AreaRegistration registration = (AreaRegistration)Activator.CreateInstance(typeof(T));
            AreaRegistrationContext registrationContext = new AreaRegistrationContext(registration.AreaName, routes, state);
            string areaNamespace = registration.GetType().Namespace;
            if (!String.IsNullOrEmpty(areaNamespace))
                registrationContext.Namespaces.Add(areaNamespace + ".*");
            registration.RegisterArea(registrationContext);
        }

        private void RegisterAreas(RouteCollection routes)
        {
            // AreaRegistration.RegisterAllAreas();
            routes.MapRoute(
                "MyArea_Default",
                "MyArea/{controller}/{action}/{id}",
                new { controller = "App", action = "Index", id = UrlParameter.Optional },
                new string[] { "MyProject.Areas.*" }
            ).DataTokens.Add("Area", "CDR");
        }

        protected void Application_Start()
        {
            //Replace AreaRegistration.RegisterAllAreas(); with lines like those
            //RegisterArea<FirstAreaRegistration>(RouteTable.Routes, null);
            //RegisterArea<SecondAreaRegistration>(RouteTable.Routes, null);
            //AreaRegistration.RegisterAllAreas();
            RegisterAreas(RouteTable.Routes);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
