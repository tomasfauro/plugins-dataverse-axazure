using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace PluginsDataverse
{
    public class Plugin1_IniSolicitud : IPlugin

    {
        public void Execute(IServiceProvider serviceProvider)
        {

            // El tracing service se utiliza para registrar información de seguimiento durante la ejecución del plugin,
            // lo que es útil para depuración y diagnóstico.
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // El plugin execution context proporciona información sobre el contexto de ejecución del plugin,
            // como los parámetros de entrada, el usuario que ejecuta el plugin, etc.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // El plugin se ejecutará solo si el contexto de ejecución contiene un parámetro de entrada llamado "Target"
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                //  Esta es la entidad que se está creando o actualizando y que activó el plugin.
                Entity entity = (Entity)context.InputParameters["Target"];

                // El organization service se utiliza para interactuar con los datos de Dynamics 365,
                // como crear, actualizar o eliminar registros.
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                try
                {
                    if (!entity.Contains("cr3c0_Fechadesolicitud") || entity["cr3c0_Fechadesolicitud"] == null)
                    {
                        entity["cr3c0_Fechadesolicitud"] = DateTime.UtcNow;
                    }
                    entity["cr3c0_Estado"] = new OptionSetValue(101000001);
                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in FollowUpPlugin.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("FollowUpPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}