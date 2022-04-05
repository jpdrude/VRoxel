using UnityEngine;
using System.Collections;

namespace Fraktalia.Core.Math
{

    public static class MathUtilities
    {

        /// <summary>
        /// Checks, if the distance of 2 Vectors is smaller than a given Lenght 
        /// </summary>
        /// <param name="start">Startpoint</param>
        /// <param name="end">Endpoint</param>
        /// <param name="Lenght">The Value to Compare</param>
        /// <returns></returns>
        public static bool IsInRange(Vector2 start, Vector2 end, float Lenght)
        {
            Vector2 dist = start - end;
            if (dist.sqrMagnitude < Lenght * Lenght) return true;

            return false;

        }

        /// <summary>
        /// Returns position on UnitCircle
        /// </summary>
        /// <param name="Winkel">Angle in degree</param>
        /// <returns></returns>
        public static Vector2 kreisPosition(float Angle)
        {
            while (Angle > 360)
            {
                Angle -= 360;
            }

            Vector2 output = new Vector2();
            output.y = Mathf.Sin(Angle * Mathf.PI / 180);
            output.x = Mathf.Cos(Angle * Mathf.PI / 180);
            return output;
        }

        public static Vector3 spherePosition(float AngleX, float AngleY)
        {
            AngleX *= Mathf.Deg2Rad;
            AngleY *= Mathf.Deg2Rad;

            Vector3 output = new Vector3();
            output.z = Mathf.Cos(AngleX) * Mathf.Sin(AngleY);
            output.x = Mathf.Sin(AngleX) * Mathf.Sin(AngleY);
            output.y = Mathf.Cos(AngleY);

            return output;
        }

        public static Vector3 RandomVector(Vector3 first, Vector3 end)
        {
            Vector3 output = new Vector3();
            output.x = Random.Range(first.x, end.x);
            output.y = Random.Range(first.y, end.y);
            output.z = Random.Range(first.z, end.z);
            return output;
        }

		public static Vector3Int Convert1DTo3D(int i, int max_x, int max_y, int max_z )
		{
			int x = i % max_x;
			int y = (i / max_x) % max_y;
			int z = i / (max_x * max_y);
			return new Vector3Int(x, y, z);
		}

		public static Vector2Int Convert1DTo2D(int i, int max_x, int max_y)
		{
			int x = i % max_x;
			int y = (i / max_x) % max_y;
			
			return new Vector2Int(x, y);
		}

		public static int Convert2DTo1D(int x, int y, int max_x)
		{
			return x + y * max_x;
		}

		public static int Convert3DTo1D(int x, int y, int z, int max_x, int max_y, int max_z)
		{
			return x + y * max_x + z * max_x * max_y;
		}

		public static float TriangleArea(Vector3 n1, Vector3 n2, Vector3 n3)
		{
			float res = Mathf.Pow(((n2.x * n1.y) - (n3.x * n1.y) - (n1.x * n2.y) + (n3.x * n2.y) + (n1.x * n3.y) - (n2.x * n3.y)), 2.0f);
			res += Mathf.Pow(((n2.x * n1.z) - (n3.x * n1.z) - (n1.x * n2.z) + (n3.x * n2.z) + (n1.x * n3.z) - (n2.x * n3.z)), 2.0f);
			res += Mathf.Pow(((n2.y * n1.z) - (n3.y * n1.z) - (n1.y * n2.z) + (n3.y * n2.z) + (n1.y * n3.z) - (n2.y * n3.z)), 2.0f);
			return Mathf.Sqrt(res) * 0.5f;
		}
	}
}
