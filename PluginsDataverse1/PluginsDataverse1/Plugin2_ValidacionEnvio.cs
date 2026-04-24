using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PluginsDataverse
{
    public class Plugin2_ValidacionEnvio : IPlugin

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
                    // Verificar la profundidad del plugin para evitar bucles infinitos
                    if (context.Depth > 1)
                    return;



                    //Tracing para verificar que el plugin se está ejecutando correctamente
                    tracingService.Trace("Plugin2_ValidacionEnvio se está ejecutando. Profundidad: {0}", context.Depth);
                    // Verificar si el campo "cr3c0_estado" tiene el valor de opción 101000002
                    if (entity.Contains("cr3c0_estado") && ((OptionSetValue)entity["cr3c0_estado"]).Value == 101000002)
                    {

                        // Recuperar los valores de los campos "cr3c0_tiposolicitud", "cr3c0_costeestimado" y "cr3c0_justificacion" de la entidad con la imagen actualizada
                        // Recuperar la PreImage
                        Entity solicitud = null;

                        if (context.PreEntityImages.Contains("PreImage"))
                        {
                            solicitud = context.PreEntityImages["PreImage"];
                        }
                        else
                        {
                            tracingService.Trace("No se encontró la PreImage");
                            return;
                        }




                        //Leer el tipo de solcitud
                        OptionSetValue tipoSolicitud = solicitud.GetAttributeValue<OptionSetValue>("cr3c0_tipodesolicitud");


                        // Si el tipo de solicitud es "Tipo 1" (valor de opción 10001), se requiere que el campo "cr3c0_costeestimado" esté completo
                        if (tipoSolicitud != null && tipoSolicitud.Value == 10001)
                        {
                            //Tracing para verificar el valor del campo "cr3c0_costeestimado" 
                            tracingService.Trace("Valor del campo 'cr3c0_costeestimado': {0}", solicitud.GetAttributeValue<Money>("cr3c0_costeestimado")?.Value);

                            Money costeEstimado = solicitud.GetAttributeValue<Money>("cr3c0_costeestimado");
                            if (costeEstimado == null || costeEstimado.Value <= 0)
                            
                            {
                                // Si el campo "cr3c0_costeestimado" está incompleto o tiene un valor no válido, se lanza una excepción para evitar el envío
                                throw new InvalidPluginExecutionException("No se puede enviar la solicitud. Por favor, complete el campo 'Coste Estimado' con un valor válido.");
                            }

                        }     // SI tipo == Cambio (10002): validar justificación 
                        else if (tipoSolicitud != null && tipoSolicitud.Value == 10002)
                        {
                            //Tracing para verificar el valor del campo "cr3c0_justificacion" 
                            tracingService.Trace("Valor del campo 'cr3c0_justificacion': {0}", solicitud.GetAttributeValue<string>("cr3c0_justificacion"));
                           
                            
                            // Si el tipo de solicitud es "Tipo 2" (valor de opción 10002), se requiere que el campo "cr3c0_justificacion" esté completo
                            string justificacion = solicitud.GetAttributeValue<string>("cr3c0_justificacion");
                          
                            // Si el campo "cr3c0_justificacion" está incompleto, se lanza una excepción para evitar el envío
                            if (string.IsNullOrWhiteSpace(justificacion)) 
                            {
                                throw new InvalidPluginExecutionException("No se puede enviar la solicitud. Por favor, complete el campo 'Justificación'.");
                            }

                        }

                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    tracingService.Trace("FaultException: {0}", ex.Detail.Message); 
                    throw new InvalidPluginExecutionException("Guardado Bloqueado, ocurrio un error.", ex);
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