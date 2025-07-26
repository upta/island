public static partial class Module
{
    [Table(
        Name = "UpdatePositionSchedules",
        Scheduled = nameof(UpdatePosition),
        ScheduledAt = nameof(ScheduledAt)
    )]
    public partial struct UpdatePositionSchedule
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;

        public ScheduleAt ScheduledAt;
    }

    [Reducer]
    public static void UpdatePosition(ReducerContext ctx, UpdatePositionSchedule scheduleArgs)
    {
        // Security check to ensure only the scheduler can call this
        if (!ctx.Sender.Equals(ctx.Identity))
        {
            throw new Exception("UpdatePosition may only be called by the scheduler");
        }

        // 1 degree in radians
        const float angleIncrement = (float)(Math.PI / 180.0);

        foreach (var entity in ctx.Db.Entities.Iter())
        {
            // Calculate current angle based on position
            float currentAngle = (float)Math.Atan2(entity.Position.Z, entity.Position.X);

            // Move clockwise by subtracting the angle increment
            float newAngle = currentAngle - angleIncrement;

            // Calculate new position on circle with radius 5
            const float radius = 5.0f;
            float newX = radius * (float)Math.Cos(newAngle);
            float newZ = radius * (float)Math.Sin(newAngle);

            // Update the entity position (keep Y unchanged)
            var updatedEntity = entity;
            updatedEntity.Position = new DbVector3(newX, entity.Position.Y, newZ);

            // Update using the primary key
            ctx.Db.Entities.EntityId.Update(updatedEntity);
        }

        ctx.Db.UpdatePositionSchedules.Insert(
            new() { ScheduledAt = new ScheduleAt.Interval(TimeSpan.FromMilliseconds(100)) }
        );
    }
}
