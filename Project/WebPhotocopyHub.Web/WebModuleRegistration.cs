using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using WebPhotocopyHub.Web.Admin;
using WebPhotocopyHub.Web.Customer;
using WebPhotocopyHub.Web.Shop;

namespace WebPhotocopyHub.Web;

public static class WebModuleRegistration
{
    public static IMvcBuilder AddWebPhotocopyHubWebModules(this IMvcBuilder p_objBuilder)
    {
        Add_Module(p_objBuilder, typeof(WebAdminModuleMarker));
        Add_Module(p_objBuilder, typeof(WebAdminShopModuleMarker));
        Add_Module(p_objBuilder, typeof(WebCustomerModuleMarker));

        return p_objBuilder;
    }

    private static void Add_Module(IMvcBuilder p_objBuilder, Type p_objMarkerType)
    {
        var v_asmModule = p_objMarkerType.Assembly;
        if (p_objBuilder.PartManager.ApplicationParts
            .OfType<AssemblyPart>()
            .Any(p_objPart => p_objPart.Assembly == v_asmModule))
        {
            return;
        }

        p_objBuilder.AddApplicationPart(v_asmModule);
    }
}