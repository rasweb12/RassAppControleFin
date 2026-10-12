namespace RassAppControleFin.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : notnull;
}
