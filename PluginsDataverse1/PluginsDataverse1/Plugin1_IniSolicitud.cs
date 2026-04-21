using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace PluginsDataverse
{
    public class Plugin1_IniSolicitud : IPlugin

    {
        public void Execute(IServiceProvider serviceProvider)
        {

            // El tracing service se utiliza para registrar información de seguimiento
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // El plugin execution context proporciona información sobre el contexto de ejecución del plugin, como los parámetros de entrada, el usuario que ejecuta el plugin, etc.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));


            tracingService.Trace("Ejecutandose plugin");
            if (context.Depth > 1)
                return;
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

                    if (!entity.Contains("cr3c0_fechadesolicitud") || entity["cr3c0_fechadesolicitud"] == null)
                    {
                        tracingService.Trace("Actualizando Fecha");
                        entity["cr3c0_fechadesolicitud"] = DateTime.UtcNow.Date;
                    }
                    entity["cr3c0_estado"] = new OptionSetValue(101000001);



                    tracingService.Trace("FollowUpPlugin: Updated entity with new values.");
                    // Actualiza la entidad con los nuevos valores establecidos


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
                finally
                {
                    tracingService.Trace("Fecha Solicitud : {0}", entity["cr3c0_fechadesolicitud"]);
                }
            }
        }
    }
}