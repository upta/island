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

            // Calculate velocity based on position change
            // For circular motion, tangential velocity is perpendicular to radius
            // Moving clockwise: velocity = (-sin(angle), 0, cos(angle)) * angular_speed * radius
            float angularSpeed = angleIncrement; // radians per update cycle
            float linearSpeed = angularSpeed * radius;
            float velocityX = -linearSpeed * (float)Math.Sin(newAngle);
            float velocityZ = linearSpeed * (float)Math.Cos(newAngle);

            // Calculate angular velocity (rotation around Y-axis for circular motion)
            float angularVelocityY = -angularSpeed; // negative for clockwise rotation

            // Update the entity position (keep Y unchanged)
            var updatedEntity = entity;
            updatedEntity.Position = new DbVector3(newX, entity.Position.Y, newZ);
            updatedEntity.Velocity = new DbVector3(velocityX, 0.0f, velocityZ);
            updatedEntity.AngularVelocity = new DbVector3(0.0f, angularVelocityY, 0.0f);
            updatedEntity.LastUpdated = ctx.Timestamp;

            // Update using the primary key
            ctx.Db.Entities.EntityId.Update(updatedEntity);
        }
    }
}
