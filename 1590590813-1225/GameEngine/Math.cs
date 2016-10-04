using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// Summary description for GameMath.
	/// </summary>
	public class GameMath
	{
		public GameMath()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public static Vector3 ComputeFaceNormal( Vector3 p1, Vector3 p2, Vector3 p3 )
		{
			Vector3 Normal;

			Vector3 V1 = Vector3.Subtract(p1,p2);
			Vector3 V2 = Vector3.Subtract(p3,p1);
			Normal = Vector3.Cross( V1, V2 );
			Normal.Normalize();

			return Normal;
		}

		public static bool InRect( Rectangle rect, Vector3 pt)
		{
			bool inside =  pt.X >= rect.Left && pt.X <= rect.Right && 
				           pt.Z >= rect.Bottom && pt.Z <= rect.Top;

			return inside;
		}
	}
}
