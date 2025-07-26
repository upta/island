using UnityEngine;

namespace SpacetimeDB.Types
{
    public partial class DbVector3
    {
        public static implicit operator Vector3(DbVector3 vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static implicit operator DbVector3(Vector3 vec)
        {
            return new DbVector3(vec.x, vec.y, vec.z);
        }
    }

    public partial class DbVector4
    {
        public static implicit operator Vector4(DbVector4 vec)
        {
            return new Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static implicit operator DbVector4(Vector4 vec)
        {
            return new DbVector4(vec.x, vec.y, vec.z, vec.w);
        }

        public static implicit operator Quaternion(DbVector4 vec)
        {
            return new Quaternion(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static implicit operator DbVector4(Quaternion quat)
        {
            return new DbVector4(quat.x, quat.y, quat.z, quat.w);
        }
    }
}
