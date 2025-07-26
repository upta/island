public static partial class Module
{
    [Reducer(ReducerKind.Init)]
    public static void Init(ReducerContext ctx)
    {
        var currentTime = ctx.Timestamp;

        ctx.Db.Entities.Insert(
            new()
            {
                Position = new DbVector3(0, 1, 5),
                Rotation = new DbVector4(0, 0, 0, 1),
                Velocity = new DbVector3(0, 0, 0),
                AngularVelocity = new DbVector3(0, 0, 0),
                LastUpdated = currentTime,
            }
        );

        ctx.Db.Entities.Insert(
            new()
            {
                Position = new DbVector3(0, 1, -5),
                Rotation = new DbVector4(0, 0, 0, 1),
                Velocity = new DbVector3(0, 0, 0),
                AngularVelocity = new DbVector3(0, 0, 0),
                LastUpdated = currentTime,
            }
        );

        ctx.Db.Entities.Insert(
            new()
            {
                Position = new DbVector3(5, 1, 0),
                Rotation = new DbVector4(0, 0, 0, 1),
                Velocity = new DbVector3(0, 0, 0),
                AngularVelocity = new DbVector3(0, 0, 0),
                LastUpdated = currentTime,
            }
        );

        ctx.Db.Entities.Insert(
            new()
            {
                Position = new DbVector3(-5, 1, 0),
                Rotation = new DbVector4(0, 0, 0, 1),
                Velocity = new DbVector3(0, 0, 0),
                AngularVelocity = new DbVector3(0, 0, 0),
                LastUpdated = currentTime,
            }
        );

        ctx.Db.UpdatePositionSchedules.Insert(
            new() { ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(100)) }
        );
    }
}
