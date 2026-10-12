namespace RassAppControleFin.Domain.Common;

public abstract class Entity
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Entity ID must not be empty.", nameof(id));

        Id = id;
    }

    public Guid Id { get; private set; }
}
