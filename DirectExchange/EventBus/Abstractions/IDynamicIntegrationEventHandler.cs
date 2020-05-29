using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    public interface IDynamicIntegrationEventHandler
    {
        Task HandleAsync(dynamic eventData);
    }
}