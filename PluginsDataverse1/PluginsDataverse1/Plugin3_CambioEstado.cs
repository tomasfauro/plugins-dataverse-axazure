using Microsoft.Xrm.Sdk;
using System;
using System.ServiceModel;

namespace PluginsDataverse
{
    public class Plugin3_CambioEstado : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtener el servicio de tracing para registrar información de seguimiento durante la ejecución del plugin.
            ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            
            // Obtener el contexto de ejecución del plugin.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));


            // Evitar bucles infinitos: el plugin solo se ejecutará en la primera profundidad de ejecución.
            if (context.Depth > 1) return;
            if (!context.InputParameters.Contains("Target")) return;
            if (!(context.InputParameters["Target"] is Entity target)) return;
            if (!target.Contains("cr3c0_estado")) return;

            // Obtener el servicio de organización para interactuar con los datos de Dynamics 365.
            IOrganizationServiceFactory factory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);


           
            try
            {
                // Verificar que las imágenes PreImage y PostImage estén disponibles.
                if (!context.PreEntityImages.Contains("PreImage") || !context.PostEntityImages.Contains("PostImage"))
                    throw new InvalidPluginExecutionException("Faltan imagenes del plugin (PreImage/PostImage).");

                // Obtener las imágenes PreImage y PostImage para comparar los cambios en el estado.
                Entity pre = context.PreEntityImages["PreImage"];
                Entity post = context.PostEntityImages["PostImage"];

                // Obtener los valores del campo "cr3c0_estado" en ambas imágenes para determinar el cambio de estado.
                int? estadoAnterior = pre.GetAttributeValue<OptionSetValue>("cr3c0_estado")?.Value;
                int? estadoNuevo = post.GetAttributeValue<OptionSetValue>("cr3c0_estado")?.Value;
                if (!estadoAnterior.HasValue || !estadoNuevo.HasValue) return;

                // El plugin solo se ejecutará si el estado cambia de "Borrador" (101000001) a "Enviada" (101000002).
                if (!(estadoAnterior.Value == 101000001 && estadoNuevo.Value == 101000002)) return;

                // Validación adicional: si la solicitud es urgente, debe tener una fecha límite.
                bool esUrgente = post.GetAttributeValue<bool?>("cr3c0_urgente") ?? false;
                DateTime? fechaLimite = post.GetAttributeValue<DateTime?>("cr3c0_fechalimite");


                // Si la solicitud es urgente pero no tiene fecha límite, se lanza una excepción para evitar que el cambio de estado se complete.
                if (esUrgente && !fechaLimite.HasValue)
                    throw new InvalidPluginExecutionException("Las solicitudes urgentes deben tener Fecha limite.");

                // Observacion automatica
                Entity update = new Entity(target.LogicalName, target.Id);
                update["cr3c0_observaciones"] = "Solicitud enviada a revision automaticamente.";
                service.Update(update);

                tracing.Trace("Plugin3 finalizado correctamente");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                tracing.Trace("FaultException: {0}", ex.Detail?.Message);
                throw new InvalidPluginExecutionException("Error de Dataverse en Plugin3.", ex);
            }
            catch (Exception ex)
            {
                tracing.Trace("Exception: {0}", ex);
                throw;
            }
        }
    }
}