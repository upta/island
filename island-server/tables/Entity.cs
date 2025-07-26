[Table(Name = "Entities", Public = true)]
public partial struct Entity
{
    [PrimaryKey, AutoInc]
    public int EntityId;

    public DbVector3 Position;

    public DbVector4 Rotation;

    public DbVector3 Velocity;

    public DbVector3 AngularVelocity;
}
